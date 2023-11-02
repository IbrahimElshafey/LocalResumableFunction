using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.Testing;

namespace Tests;

public class ComplexApproval
{
    [Fact]
    public async Task ComplexApproval_Test()
    {
        using var test = new TestShell(nameof(ComplexApproval_Test), typeof(Test));
        await test.ScanTypes("ComplexApproval");
        Assert.Empty(await test.RoundCheck(0, 0, 0));

        var instance = new Test();
        var requestId = instance.RequestAdded("New Request");
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 0, MemberRole.MemberOne));
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 0, MemberRole.MemberTwo));
        instance.ChefSkipTopic(new RequestTopicIndex(requestId, 0, MemberRole.Chef));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 0, MemberRole.Chef));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 1, MemberRole.MemberTwo));
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 1, MemberRole.MemberThree));
        instance.ChefSkipTopic(new RequestTopicIndex(requestId, 1, MemberRole.Chef));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 1, MemberRole.Chef));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.MemberOne));
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.MemberTwo));
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.MemberThree));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.Chef));

        instance.ChefFinalApproval(requestId);
        var errors = await test.GetLogs();
        Assert.Empty(await test.RoundCheck(14, -1, 1));
    }


    public class Test : ResumableFunctionsContainer
    {

        public const int CommitteeMembersCount = 3;
        public const int TopicsCount = 3;
        public int RequestId { get; set; }
        public bool FinalDecision { get; set; }

        public int PublicCounter { get; set; } = 10;

        [ResumableFunctionEntryPoint("ComplexApproval")]
        public async IAsyncEnumerable<Wait> ComplexApproval()
        {
            yield return
                Wait<string, int>(RequestAdded, "Request Added")
                    .AfterMatch((request, requestId) => RequestId = requestId);

            for (var currentTopicIndex = 0; currentTopicIndex < TopicsCount; currentTopicIndex++)
            {
                yield return
                    Wait($"Wait all committee approve topic {currentTopicIndex} or manager skip",
                        new[]
                        {
                            AllCommitteeApproveTopic(currentTopicIndex),
                            ChefSkipTopic(currentTopicIndex)
                        })
                        .MatchAny();

                //closure is different than locals for this wait
                yield return ChefTopicApproval(currentTopicIndex);
            }

            yield return await FinalApproval();
        }

        private Wait ChefTopicApproval(int chefTopicIndex)
        {
            AskMemberToApproveTopic(RequestId, chefTopicIndex, MemberRole.Chef);
            return
                Wait<RequestTopicIndex, string>(MemberApproveRequest, $"Chef Topic {chefTopicIndex} Approval")
                    .MatchIf((topicIndex, decision) =>
                        topicIndex.RequestId == RequestId &&
                        topicIndex.TopicIndex == chefTopicIndex &&
                        topicIndex.MemberRole == MemberRole.Chef)
                    .NothingAfterMatch();
        }

        private void AskMemberToApproveTopic(int requestId, int currentTopicIndex, MemberRole memberRole)
        {
            return;
        }

        private async Task<Wait> FinalApproval()
        {
            await AskChefToApproveRequest(RequestId);
            return Wait<int, bool>(ChefFinalApproval, "Chef Final Approval")
                .MatchIf((requestId, decision) => requestId == RequestId)
                .AfterMatch((requestId, decision) => FinalDecision = decision);
        }

        private async Task AskChefToApproveRequest(int requestId)
        {
            await Task.Delay(100);
        }


        private Wait ChefSkipTopic(int chefSkipTopicIndex)
        {
            var skipCounter = 10;
            return Wait<RequestTopicIndex, string>
                (ChefSkipTopic, $"Chef Skip Topic {chefSkipTopicIndex} Approval")
                .MatchIf((topicIndex, _) =>
                    topicIndex.RequestId == RequestId &&
                    topicIndex.TopicIndex == chefSkipTopicIndex)
                .AfterMatch((_, _) => skipCounter += 5);
        }

        private Wait AllCommitteeApproveTopic(int membersTopicIndex)
        {
            var waits = new Wait[3];
            int sharedCounter = 10;
            MemberRole currentRole = MemberRole.None;
            for (var memberIndex = 0; memberIndex < CommitteeMembersCount; memberIndex++)
            {
                currentRole = (MemberRole)memberIndex;
                AskMemberToApproveTopic(RequestId, membersTopicIndex, currentRole);
                waits[memberIndex] =
                    Wait<RequestTopicIndex, string>(
                        MemberApproveRequest, $"{currentRole} Topic {membersTopicIndex} Approval")
                    .MatchIf((topicIndex, _) =>
                        topicIndex.RequestId == RequestId &&
                        topicIndex.TopicIndex == membersTopicIndex &&
                        topicIndex.MemberRole == currentRole)
                    //.NothingAfterMatch()
                    .AfterMatch((input, output) =>
                    {
                        sharedCounter += 10;
                        if (sharedCounter < 10)
                            throw new Exception("Local var `sharedCounter` must be >= 10.");
                        PublicCounter = 1000;
                    })
                    ;
            }
            Console.WriteLine(currentRole);
            return
                Wait($"Wait All Committee to Approve Topic {membersTopicIndex}", waits)
                .MatchIf((_) =>
                {
                    bool result = sharedCounter == 40;
                    if (result) sharedCounter += 5;
                    PublicCounter = 2000;
                    return result;
                });
        }


        [PushCall("RequestAdded")]
        public int RequestAdded(string request) => Random.Shared.Next();

        [PushCall("MemberApproveRequest")]
        public string MemberApproveRequest(RequestTopicIndex topicIndex) => $"Request {topicIndex.RequestId}:{topicIndex.TopicIndex} approved.";
        [PushCall("ChefSkipTopic")] public string ChefSkipTopic(RequestTopicIndex topicIndex) => $"Chef skipped topic {topicIndex.RequestId}:{topicIndex.TopicIndex}.";
        [PushCall("ChefFinalApproval")] public bool ChefFinalApproval(int requestId) => true;

    }

    public record RequestTopicIndex(int RequestId, int TopicIndex, MemberRole MemberRole);
    public enum MemberRole
    {
        None = -1,
        Chef = 3,
        MemberOne = 0,
        MemberTwo = 1,
        MemberThree = 2,
    }
}
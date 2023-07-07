using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;
using static Tests.ComplexApproval;

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
        instance.ChefSkipTopic(new RequestTopicIndex(requestId, 0, MemberRole.MemberTwo));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 0, MemberRole.Chef));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 1, MemberRole.MemberTwo));
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 1, MemberRole.MemberThree));
        instance.ChefSkipTopic(new RequestTopicIndex(requestId, 1, MemberRole.MemberTwo));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 1, MemberRole.Chef));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.MemberOne));
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.MemberTwo));
        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.MemberThree));

        instance.MemberApproveRequest(new RequestTopicIndex(requestId, 2, MemberRole.Chef));

        instance.ChefFinalApproval(requestId);
        var errors = await test.GetLogs();
        Assert.Empty(await test.RoundCheck(14, -1, 1));
    }


    public class Test : ResumableFunction
    {

        public const int CommitteeMembersCount = 3;
        public const int TopicsCount = 3;
        public int RequestId { get; set; }
        public int CurrentTopicIndex { get; set; }
        public bool FinalDecision { get; set; }

        [ResumableFunctionEntryPoint("ComplexApproval")]
        public async IAsyncEnumerable<Wait> ComplexApproval()
        {
            yield return
                Wait<string, int>("Request Added", RequestAdded)
                    .SetData((request, requestId) => RequestId == requestId);

            for (; CurrentTopicIndex < TopicsCount; CurrentTopicIndex++)
            {
                yield return
                    Wait($"Wait all committee approve topic {CurrentTopicIndex} or manager skip",
                        AllCommitteeApproveTopic(),
                        ChefSkipTopic())
                        .First();

                yield return ChefTopicApproval();
            }

            yield return await FinalApproval();
        }

        private Wait ChefTopicApproval()
        {
            AskMemberToApproveTopic(RequestId, CurrentTopicIndex, MemberRole.Chef);
            return
                Wait<RequestTopicIndex, string>($"Chef Topic {CurrentTopicIndex} Approval", MemberApproveRequest)
                    .MatchIf((topicIndex, decision) =>
                        topicIndex.RequestId == RequestId &&
                        topicIndex.TopicIndex == CurrentTopicIndex &&
                        topicIndex.MemberRole == MemberRole.Chef)
                    .NoSetData();
        }

        private void AskMemberToApproveTopic(int requestId, int currentTopicIndex, MemberRole memberRole)
        {
            return;
        }

        private async Task<Wait> FinalApproval()
        {
            await AskChefToApproveRequest(RequestId);
            return Wait<int, bool>("Chef Final Approval", ChefFinalApproval)
                .MatchIf((requestId, decision) => requestId == RequestId)
                .SetData((requestId, decision) => FinalDecision == decision);
        }

        private async Task AskChefToApproveRequest(int requestId)
        {
            await Task.Delay(100);
        }


        private MethodWait ChefSkipTopic()
        {
            return Wait<RequestTopicIndex, string>
                ($"Chef Skip Topic {CurrentTopicIndex} Approval", ChefSkipTopic)
                .MatchIf((topicIndex, decision) =>
                    topicIndex.RequestId == RequestId &&
                    topicIndex.TopicIndex == CurrentTopicIndex);
        }

        private Wait AllCommitteeApproveTopic()
        {
            var waits = new Wait[3];
            for (var memberIndex = 0; memberIndex < CommitteeMembersCount; memberIndex++)
            {
                var currentMember = (MemberRole)memberIndex;
                AskMemberToApproveTopic(RequestId, CurrentTopicIndex, currentMember);
                waits[memberIndex] =
                    Wait<RequestTopicIndex, string>($"{currentMember} Topic {CurrentTopicIndex} Approval", MemberApproveRequest)
                    .MatchIf((topicIndex, decision) =>
                        topicIndex.RequestId == RequestId &&
                        topicIndex.TopicIndex == CurrentTopicIndex &&
                        topicIndex.MemberRole == Computed(currentMember))
                    .NoSetData();
            }

            return Wait($"Wait All Committee to Approve Topic {CurrentTopicIndex}", waits);
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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core
{
    internal partial class WaitsProcessor : IWaitsProcessor
    {


        [DisplayName("Process Function Expected Matches where `FunctionId:{0}`, `PushedCallId:{1}`, `MethodGroupId:{2}`")]
        public async Task ProcessFunctionExpectedWaitMatchesV2(int functionId, int pushedCallId, int methodGroupId)
        {
            await _backgroundJobExecutor.Execute(
                $"ProcessFunctionExpectedMatchedWaits_{functionId}_{pushedCallId}",
                async () =>
                {
                    _pushedCall = await LoadPushedCall(pushedCallId);
                    var waitTemplates = await _templatesRepo.GetWaitTemplates(methodGroupId, functionId);
                    var matchExist = false;
                    foreach (var template in waitTemplates)
                    {
                        var waits = await _waitsRepo.GetWaitsForTemplate(
                            template,
                            template.GetMandatoryPart(_pushedCall.DataValue),
                            x => x.RequestedByFunction,
                            x => x.FunctionState);
                        foreach (var wait in waits)
                        {
                            await LoadWaitProps(wait);
                            wait.Template = template;
                            _waitCall = _waitProcessingRecordsRepo.Add(new WaitProcessingRecord
                            {
                                FunctionId = functionId,
                                PushedCallId = pushedCallId,
                                ServiceId = template.ServiceId,
                                WaitId = wait.Id,
                                StateId = wait.FunctionStateId
                            });
                            await _context.SaveChangesAsync();
                            _methodWait = wait;

                            var isSuccess = await Pipeline(
                            DesrializeInputOutput,
                            CheckIfMatch,
                            CloneIfFirst,
                            UpdateFunctionData,
                            ResumeExecution);

                            if (!isSuccess) continue;

                            matchExist = true;
                            break;
                        }
                        if (matchExist) break;
                    }


                },
                $"Error when process wait `{_methodWait?.Id}` that may be a match for pushed call `{pushedCallId}` and function `{functionId}`");
        }

        private async Task LoadWaitProps(MethodWait methodWait)
        {

            methodWait.MethodToWait = await _methodIdsRepo.GetMethodIdentifierById(methodWait.MethodToWaitId);

            if (methodWait.MethodToWait == null)
            {
                var error = $"No method exist that linked to wait `{methodWait.MethodToWaitId}`.";
                _logger.LogError(error);
                throw new Exception(error);
            }
            methodWait.FunctionState.LoadUnmappedProps(methodWait.RequestedByFunction.InClassType);
            methodWait.LoadUnmappedProps();
        }
    }


}

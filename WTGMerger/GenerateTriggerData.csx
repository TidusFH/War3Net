#r ".\bin\Release\net8.0\win-x64\WTGMerger.dll"
using WTGMerger;

var war3Patches = @"..\War3 Patches";
var baseTriggerData = @"..\src\War3Net.Build.Core\Resources\TriggerData.txt";
var output = "ExtendedTriggerData.txt";

TriggerDataMerger.MergeTriggerData(war3Patches, baseTriggerData, output);


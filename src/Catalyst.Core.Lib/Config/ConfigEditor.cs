using System;
using Catalyst.Abstractions.Config;
using Catalyst.Protocol.Network;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Lib.Config
{
    public abstract class ConfigEditor : IConfigEditor
    {
        public void RunConfigEditor(string dataDir,
            NetworkType networkType = NetworkType.Devnet)
        {
            foreach (var (configFileName, listOfRequiredEdits) in RequiredConfigFileEdits(networkType))
            {
                Console.WriteLine(listOfRequiredEdits.Count);
                var targetFile = Path.Combine(dataDir, configFileName);
                if (File.Exists(targetFile))
                {
                    var fileEdited = false;
                    using var r = new StreamReader(targetFile);
                    var json = r.ReadToEnd();
                    var jObject = JObject.Parse(json);
                    
                    foreach (var (keyToReplace, replacementValue) in listOfRequiredEdits)
                    {
                        var existingValue = jObject.SelectToken(keyToReplace)?.ToObject<string>();
                        if (replacementValue == existingValue) continue;
                        jObject.SelectToken(keyToReplace)?.Replace(replacementValue);
                        Console.WriteLine(replacementValue);
                        fileEdited = true;
                    }
                    
                    if (fileEdited) {File.WriteAllText(targetFile, jObject.ToString());}
                }
            }
        }
        
        protected abstract Dictionary<string, List<KeyValuePair<string, string>>> RequiredConfigFileEdits(NetworkType network);

    }
}

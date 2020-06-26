#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

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
                    var json = File.ReadAllText(targetFile);
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

#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
﻿
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Catalyst.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new AllowNonOptimized());
        }

        private class AllowNonOptimized : ManualConfig
        {
            public AllowNonOptimized()
            {
                AddValidator(JitOptimizationsValidator.DontFailOnError);
                AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
                AddExporter(DefaultConfig.Instance.GetExporters().ToArray()); 
                AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
            }
        }
    }
}


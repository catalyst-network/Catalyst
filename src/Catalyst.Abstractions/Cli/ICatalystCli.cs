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

using System.Threading;

namespace Catalyst.Abstractions.Cli
{
    public interface ICatalystCli
    {
        /// <summary>
        ///     Runs the main cli ui.
        /// </summary>
        /// <returns></returns>
        bool RunConsole(CancellationToken ct);

        /// <summary>
        /// Parses the Options object sent and calls the correct message to handle the option a defined in the MapResult
        /// </summary>
        /// <param name="args">string array including the parameters passed through the command line</param>
        /// <returns>Returns true if a method to handle the options is found otherwise returns false</returns>
        bool ParseCommand(params string[] args);
    }
}

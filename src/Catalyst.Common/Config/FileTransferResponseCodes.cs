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

using Catalyst.Common.Enumerator;

namespace Catalyst.Common.Config
{
    public class FileTransferResponseCodes : Enumeration
    {
        public static readonly FileTransferResponseCodes Successful = new SuccessfulCodeStatus();
        public static readonly FileTransferResponseCodes FileAlreadyExists = new FileAlreadyExistsStatus();
        public static readonly FileTransferResponseCodes Error = new ErrorStatus();
        public static readonly FileTransferResponseCodes Finished = new FinishedStatus();
        public static readonly FileTransferResponseCodes Expired = new ExpiredStatus();
        public static readonly FileTransferResponseCodes Failed = new FailedStatus();

        private FileTransferResponseCodes(int id, string name) : base(id, name) { }
        
        private sealed class SuccessfulCodeStatus : FileTransferResponseCodes
        {
            public SuccessfulCodeStatus() : base(1, "successful") { }
        }
        
        private sealed class FileAlreadyExistsStatus : FileTransferResponseCodes
        {
            public FileAlreadyExistsStatus() : base(2, "fileAlreadyExists") { }
        }
        
        private sealed class ErrorStatus : FileTransferResponseCodes
        {
            public ErrorStatus() : base(3, "error") { }
        }
        
        private sealed class FinishedStatus : FileTransferResponseCodes
        {
            public FinishedStatus() : base(4, "finished") { }
        }
        
        private sealed class ExpiredStatus : FileTransferResponseCodes
        {
            public ExpiredStatus() : base(5, "expired") { }
        }
        
        private sealed class FailedStatus : FileTransferResponseCodes
        {
            public FailedStatus() : base(6, "failed") { }
        }
    }
}

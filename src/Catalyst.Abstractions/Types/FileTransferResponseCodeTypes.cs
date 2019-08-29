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

using System.Linq;
using Catalyst.Abstractions.Enumerator;

namespace Catalyst.Abstractions.Types
{
    public class FileTransferResponseCodeTypes : Enumeration
    {
        public static readonly FileTransferResponseCodeTypes Successful = new SuccessfulCodeTypeStatus();
        public static readonly FileTransferResponseCodeTypes TransferPending = new TransferPendingStatus();
        public static readonly FileTransferResponseCodeTypes Error = new ErrorStatus();
        public static readonly FileTransferResponseCodeTypes Finished = new FinishedStatus();
        public static readonly FileTransferResponseCodeTypes Expired = new ExpiredStatus();
        public static readonly FileTransferResponseCodeTypes Failed = new FailedStatus();

        private FileTransferResponseCodeTypes(int id, string name) : base(id, name) { }
        
        private sealed class SuccessfulCodeTypeStatus : FileTransferResponseCodeTypes
        {
            public SuccessfulCodeTypeStatus() : base(1, "successful") { }
        }
        
        private sealed class TransferPendingStatus : FileTransferResponseCodeTypes
        {
            public TransferPendingStatus() : base(2, "transferPending") { }
        }
        
        private sealed class ErrorStatus : FileTransferResponseCodeTypes
        {
            public ErrorStatus() : base(3, "error") { }
        }
        
        private sealed class FinishedStatus : FileTransferResponseCodeTypes
        {
            public FinishedStatus() : base(4, "finished") { }
        }
        
        private sealed class ExpiredStatus : FileTransferResponseCodeTypes
        {
            public ExpiredStatus() : base(5, "expired") { }
        }
        
        private sealed class FailedStatus : FileTransferResponseCodeTypes
        {
            public FailedStatus() : base(6, "failed") { }
        }

        public static explicit operator FileTransferResponseCodeTypes(byte id)
        {
            return GetAll<FileTransferResponseCodeTypes>().Single(f => f.Id == id);
        }
    }
}

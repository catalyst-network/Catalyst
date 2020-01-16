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

// The SHA3 doesn't create .Net Standard package.
// This is a copy of https://bitbucket.org/jdluzen/sha3/raw/d1fd55dc225d18a7fb61515b62d3c8f164d2e788/SHA3Managed/SHA3Managed.cs

using System;

namespace MultiFormats.Cryptography
{
    internal partial class KeccakManaged : Keccak
    {
        public KeccakManaged(int hashBitLength)
            : base(hashBitLength) { }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            base.HashCore(array, ibStart, cbSize);
            if (cbSize == 0)
                return;
            var sizeInBytes = SizeInBytes;
            if (buffer == null)
                buffer = new byte[sizeInBytes];
            var stride = sizeInBytes >> 3;
            var utemps = new ulong[stride];
            if (buffLength == sizeInBytes)
                throw new Exception("Unexpected error, the internal buffer is full");
            AddToBuffer(array, ref ibStart, ref cbSize);
            if (buffLength == sizeInBytes) //buffer full
            {
                Buffer.BlockCopy(buffer, 0, utemps, 0, sizeInBytes);
                KeccakF(utemps, stride);
                buffLength = 0;
            }

            for (; cbSize >= sizeInBytes; cbSize -= sizeInBytes, ibStart += sizeInBytes)
            {
                Buffer.BlockCopy(array, ibStart, utemps, 0, sizeInBytes);
                KeccakF(utemps, stride);
            }

            if (cbSize > 0) //some left over
            {
                Buffer.BlockCopy(array, ibStart, buffer, buffLength, cbSize);
                buffLength += cbSize;
            }
        }

        protected override byte[] HashFinal()
        {
            var sizeInBytes = SizeInBytes;
            var outb = new byte[HashByteLength];

            //    padding
            if (buffer == null)
                buffer = new byte[sizeInBytes];
            else
                Array.Clear(buffer, buffLength, sizeInBytes - buffLength);
            buffer[buffLength++] = 1;
            buffer[sizeInBytes - 1] |= 0x80;
            var stride = sizeInBytes >> 3;
            var utemps = new ulong[stride];
            Buffer.BlockCopy(buffer, 0, utemps, 0, sizeInBytes);
            KeccakF(utemps, stride);
            Buffer.BlockCopy(state, 0, outb, 0, HashByteLength);
            return outb;
        }

        private void KeccakF(ulong[] inb, int laneCount)
        {
            while (--laneCount >= 0)
                state[laneCount] ^= inb[laneCount];
            ulong Aba, Abe, Abi, Abo, Abu;
            ulong Aga, Age, Agi, Ago, Agu;
            ulong Aka, Ake, Aki, Ako, Aku;
            ulong Ama, Ame, Ami, Amo, Amu;
            ulong Asa, Ase, Asi, Aso, Asu;
            ulong BCa, BCe, BCi, BCo, BCu;
            ulong Da, De, Di, Do, Du;
            ulong Eba, Ebe, Ebi, Ebo, Ebu;
            ulong Ega, Ege, Egi, Ego, Egu;
            ulong Eka, Eke, Eki, Eko, Eku;
            ulong Ema, Eme, Emi, Emo, Emu;
            ulong Esa, Ese, Esi, Eso, Esu;
            var round = laneCount;

            //copyFromState(A, state)
            Aba = state[0];
            Abe = state[1];
            Abi = state[2];
            Abo = state[3];
            Abu = state[4];
            Aga = state[5];
            Age = state[6];
            Agi = state[7];
            Ago = state[8];
            Agu = state[9];
            Aka = state[10];
            Ake = state[11];
            Aki = state[12];
            Ako = state[13];
            Aku = state[14];
            Ama = state[15];
            Ame = state[16];
            Ami = state[17];
            Amo = state[18];
            Amu = state[19];
            Asa = state[20];
            Ase = state[21];
            Asi = state[22];
            Aso = state[23];
            Asu = state[24];

            for (round = 0; round < KeccakNumberOfRounds; round += 2)
            {
                //    prepareTheta
                BCa = Aba ^ Aga ^ Aka ^ Ama ^ Asa;
                BCe = Abe ^ Age ^ Ake ^ Ame ^ Ase;
                BCi = Abi ^ Agi ^ Aki ^ Ami ^ Asi;
                BCo = Abo ^ Ago ^ Ako ^ Amo ^ Aso;
                BCu = Abu ^ Agu ^ Aku ^ Amu ^ Asu;

                //thetaRhoPiChiIotaPrepareTheta(round  , A, E)
                Da = BCu ^ Rol(BCe, 1);
                De = BCa ^ Rol(BCi, 1);
                Di = BCe ^ Rol(BCo, 1);
                Do = BCi ^ Rol(BCu, 1);
                Du = BCo ^ Rol(BCa, 1);

                Aba ^= Da;
                BCa = Aba;
                Age ^= De;
                BCe = Rol(Age, 44);
                Aki ^= Di;
                BCi = Rol(Aki, 43);
                Amo ^= Do;
                BCo = Rol(Amo, 21);
                Asu ^= Du;
                BCu = Rol(Asu, 14);
                Eba = BCa ^ (~BCe & BCi);
                Eba ^= RoundConstants[round];
                Ebe = BCe ^ (~BCi & BCo);
                Ebi = BCi ^ (~BCo & BCu);
                Ebo = BCo ^ (~BCu & BCa);
                Ebu = BCu ^ (~BCa & BCe);

                Abo ^= Do;
                BCa = Rol(Abo, 28);
                Agu ^= Du;
                BCe = Rol(Agu, 20);
                Aka ^= Da;
                BCi = Rol(Aka, 3);
                Ame ^= De;
                BCo = Rol(Ame, 45);
                Asi ^= Di;
                BCu = Rol(Asi, 61);
                Ega = BCa ^ (~BCe & BCi);
                Ege = BCe ^ (~BCi & BCo);
                Egi = BCi ^ (~BCo & BCu);
                Ego = BCo ^ (~BCu & BCa);
                Egu = BCu ^ (~BCa & BCe);

                Abe ^= De;
                BCa = Rol(Abe, 1);
                Agi ^= Di;
                BCe = Rol(Agi, 6);
                Ako ^= Do;
                BCi = Rol(Ako, 25);
                Amu ^= Du;
                BCo = Rol(Amu, 8);
                Asa ^= Da;
                BCu = Rol(Asa, 18);
                Eka = BCa ^ (~BCe & BCi);
                Eke = BCe ^ (~BCi & BCo);
                Eki = BCi ^ (~BCo & BCu);
                Eko = BCo ^ (~BCu & BCa);
                Eku = BCu ^ (~BCa & BCe);

                Abu ^= Du;
                BCa = Rol(Abu, 27);
                Aga ^= Da;
                BCe = Rol(Aga, 36);
                Ake ^= De;
                BCi = Rol(Ake, 10);
                Ami ^= Di;
                BCo = Rol(Ami, 15);
                Aso ^= Do;
                BCu = Rol(Aso, 56);
                Ema = BCa ^ (~BCe & BCi);
                Eme = BCe ^ (~BCi & BCo);
                Emi = BCi ^ (~BCo & BCu);
                Emo = BCo ^ (~BCu & BCa);
                Emu = BCu ^ (~BCa & BCe);

                Abi ^= Di;
                BCa = Rol(Abi, 62);
                Ago ^= Do;
                BCe = Rol(Ago, 55);
                Aku ^= Du;
                BCi = Rol(Aku, 39);
                Ama ^= Da;
                BCo = Rol(Ama, 41);
                Ase ^= De;
                BCu = Rol(Ase, 2);
                Esa = BCa ^ (~BCe & BCi);
                Ese = BCe ^ (~BCi & BCo);
                Esi = BCi ^ (~BCo & BCu);
                Eso = BCo ^ (~BCu & BCa);
                Esu = BCu ^ (~BCa & BCe);

                //    prepareTheta
                BCa = Eba ^ Ega ^ Eka ^ Ema ^ Esa;
                BCe = Ebe ^ Ege ^ Eke ^ Eme ^ Ese;
                BCi = Ebi ^ Egi ^ Eki ^ Emi ^ Esi;
                BCo = Ebo ^ Ego ^ Eko ^ Emo ^ Eso;
                BCu = Ebu ^ Egu ^ Eku ^ Emu ^ Esu;

                //thetaRhoPiChiIotaPrepareTheta(round+1, E, A)
                Da = BCu ^ Rol(BCe, 1);
                De = BCa ^ Rol(BCi, 1);
                Di = BCe ^ Rol(BCo, 1);
                Do = BCi ^ Rol(BCu, 1);
                Du = BCo ^ Rol(BCa, 1);

                Eba ^= Da;
                BCa = Eba;
                Ege ^= De;
                BCe = Rol(Ege, 44);
                Eki ^= Di;
                BCi = Rol(Eki, 43);
                Emo ^= Do;
                BCo = Rol(Emo, 21);
                Esu ^= Du;
                BCu = Rol(Esu, 14);
                Aba = BCa ^ (~BCe & BCi);
                Aba ^= RoundConstants[round + 1];
                Abe = BCe ^ (~BCi & BCo);
                Abi = BCi ^ (~BCo & BCu);
                Abo = BCo ^ (~BCu & BCa);
                Abu = BCu ^ (~BCa & BCe);

                Ebo ^= Do;
                BCa = Rol(Ebo, 28);
                Egu ^= Du;
                BCe = Rol(Egu, 20);
                Eka ^= Da;
                BCi = Rol(Eka, 3);
                Eme ^= De;
                BCo = Rol(Eme, 45);
                Esi ^= Di;
                BCu = Rol(Esi, 61);
                Aga = BCa ^ (~BCe & BCi);
                Age = BCe ^ (~BCi & BCo);
                Agi = BCi ^ (~BCo & BCu);
                Ago = BCo ^ (~BCu & BCa);
                Agu = BCu ^ (~BCa & BCe);

                Ebe ^= De;
                BCa = Rol(Ebe, 1);
                Egi ^= Di;
                BCe = Rol(Egi, 6);
                Eko ^= Do;
                BCi = Rol(Eko, 25);
                Emu ^= Du;
                BCo = Rol(Emu, 8);
                Esa ^= Da;
                BCu = Rol(Esa, 18);
                Aka = BCa ^ (~BCe & BCi);
                Ake = BCe ^ (~BCi & BCo);
                Aki = BCi ^ (~BCo & BCu);
                Ako = BCo ^ (~BCu & BCa);
                Aku = BCu ^ (~BCa & BCe);

                Ebu ^= Du;
                BCa = Rol(Ebu, 27);
                Ega ^= Da;
                BCe = Rol(Ega, 36);
                Eke ^= De;
                BCi = Rol(Eke, 10);
                Emi ^= Di;
                BCo = Rol(Emi, 15);
                Eso ^= Do;
                BCu = Rol(Eso, 56);
                Ama = BCa ^ (~BCe & BCi);
                Ame = BCe ^ (~BCi & BCo);
                Ami = BCi ^ (~BCo & BCu);
                Amo = BCo ^ (~BCu & BCa);
                Amu = BCu ^ (~BCa & BCe);

                Ebi ^= Di;
                BCa = Rol(Ebi, 62);
                Ego ^= Do;
                BCe = Rol(Ego, 55);
                Eku ^= Du;
                BCi = Rol(Eku, 39);
                Ema ^= Da;
                BCo = Rol(Ema, 41);
                Ese ^= De;
                BCu = Rol(Ese, 2);
                Asa = BCa ^ (~BCe & BCi);
                Ase = BCe ^ (~BCi & BCo);
                Asi = BCi ^ (~BCo & BCu);
                Aso = BCo ^ (~BCu & BCa);
                Asu = BCu ^ (~BCa & BCe);
            }

            //copyToState(state, A)
            state[0] = Aba;
            state[1] = Abe;
            state[2] = Abi;
            state[3] = Abo;
            state[4] = Abu;
            state[5] = Aga;
            state[6] = Age;
            state[7] = Agi;
            state[8] = Ago;
            state[9] = Agu;
            state[10] = Aka;
            state[11] = Ake;
            state[12] = Aki;
            state[13] = Ako;
            state[14] = Aku;
            state[15] = Ama;
            state[16] = Ame;
            state[17] = Ami;
            state[18] = Amo;
            state[19] = Amu;
            state[20] = Asa;
            state[21] = Ase;
            state[22] = Asi;
            state[23] = Aso;
            state[24] = Asu;
        }
    }
}

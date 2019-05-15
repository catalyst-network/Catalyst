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

using System.Numerics;
using Catalyst.Common.Enumerator;

namespace Catalyst.Common.Config
{
    public class UnitTypes
        : Enumeration
    {
        private UnitTypes(int id, string name, string unit) : base(id, name)
        {
            UnitString = unit;
        }

        public BigInteger UnitAsBigInt => BigInteger.Parse(UnitString);

        public string UnitString { get; }
        
        public static readonly UnitTypes Fulhame = new FulhameUnit();
        public static readonly UnitTypes KitKat = new KitKatUnit();
        public static readonly UnitTypes GrumpyFace = new GrumpyFaceUnit();        
        public static readonly UnitTypes GarryLazerEyes = new GarryLazerEyesUnit();        
        public static readonly UnitTypes SteveFrench = new SteveFrenchUnit();        
        public static readonly UnitTypes YoctoKat = new YoctoKatUnit();        
        public static readonly UnitTypes ZeptoKat = new ZeptoKatUnit();   
        public static readonly UnitTypes AttoKat = new AttoKatUnit();        
        public static readonly UnitTypes FemtoKat = new FemtoKatUnit();        
        public static readonly UnitTypes PicoKat = new PicoKatUnit();        
        public static readonly UnitTypes NanoKat = new NanoKatUnit();        
        public static readonly UnitTypes MicroKat = new MicroKatUnit();        
        public static readonly UnitTypes MiliKat = new MiliKatUnit();        
        public static readonly UnitTypes CentiKat = new CentiKatUnit();        
        public static readonly UnitTypes Korin = new KorinUnit();        
        public static readonly UnitTypes DekaKat = new DekaKatUnit();        
        public static readonly UnitTypes HectoKat = new HectoKatUnit();        
        public static readonly UnitTypes Kat = new KatUnit();
        public static readonly UnitTypes MegaKat = new MegaKatUnit();        
        public static readonly UnitTypes GigaKat = new GigaKatUnit();        
        public static readonly UnitTypes Schrodinger = new SchrodingerUnit();        
        public static readonly UnitTypes TerraKat = new TerraKatUnit();        
        public static readonly UnitTypes PetaKat = new PetaKatUnit();        
        public static readonly UnitTypes FatKat = new FatKatUnit();        

        private sealed class FulhameUnit : UnitTypes
        {
            public FulhameUnit() : base(1, "Fulhame", "1") { }
        }
        
        private sealed class KitKatUnit : UnitTypes
        {
            public KitKatUnit() : base(2, "KitKat", "1000") { }
        }
        
        private sealed class GrumpyFaceUnit : UnitTypes
        {
            public GrumpyFaceUnit() : base(3, "GrumpyFace", "1000") { }
        }
        
        private sealed class GarryLazerEyesUnit : UnitTypes
        {
            public GarryLazerEyesUnit() : base(4, "GarryLazerEyes", "1000") { }
        }
        
        private sealed class SteveFrenchUnit : UnitTypes
        {
            public SteveFrenchUnit() : base(4, "StevefrenchUnit", "1000") { }
        }
        
        private sealed class YoctoKatUnit : UnitTypes
        {
            public YoctoKatUnit() : base(5, "YoctoKat", "1000000") { }
        }
        
        private sealed class ZeptoKatUnit : UnitTypes
        {
            public ZeptoKatUnit() : base(6, "ZeptoKat", "1000000") { }
        }
        
        private sealed class AttoKatUnit : UnitTypes
        {
            public AttoKatUnit() : base(7, "AttoKat", "1000000000") { }
        }
        
        private sealed class FemtoKatUnit : UnitTypes
        {
            public FemtoKatUnit() : base(8, "FemtoKat", "1000000000") { }
        }
        
        private sealed class PicoKatUnit : UnitTypes
        {
            public PicoKatUnit() : base(9, "PicoKat", "1000000000") { }
        }
        
        private sealed class NanoKatUnit : UnitTypes
        {
            public NanoKatUnit() : base(10, "NanoKat", "1000000000") { }
        }
        
        private sealed class MicroKatUnit : UnitTypes
        {
            public MicroKatUnit() : base(11, "MicroKat", "1000000000000") { }
        }
        
        private sealed class MiliKatUnit : UnitTypes
        {
            public MiliKatUnit() : base(12, "MiliKat", "1000000000000") { }
        }
        
        private sealed class CentiKatUnit : UnitTypes
        {
            public CentiKatUnit() : base(13, "CentiKat", "1000000000000") { }
        }
        
        private sealed class KorinUnit : UnitTypes
        {
            public KorinUnit() : base(14, "Korin", "1000000000000000") { }
        }
        
        private sealed class DekaKatUnit : UnitTypes
        {
            public DekaKatUnit() : base(15, "DekaKat", "1000000000000000") { }
        }
        
        private sealed class HectoKatUnit : UnitTypes
        {
            public HectoKatUnit() : base(16, "HectoKat", "1000000000000000") { }
        }
        
        private sealed class KatUnit : UnitTypes
        {
            public KatUnit() : base(17, "Kat", "1000000000000000000") { }
        }
        
        private sealed class MegaKatUnit : UnitTypes
        {
            public MegaKatUnit() : base(18, "MegaKat", "1000000000000000000000") { }
        }
        
        private sealed class GigaKatUnit : UnitTypes
        {
            public GigaKatUnit() : base(19, "GigaKat", "1000000000000000000000") { }
        }
        
        private sealed class SchrodingerUnit : UnitTypes
        {
            public SchrodingerUnit() : base(20, "Schrodinger", "1000000000000000000000") { }
        }
        
        private sealed class TerraKatUnit : UnitTypes
        {
            public TerraKatUnit() : base(21, "TerraKat", "1000000000000000000000000") { }
        }
        
        private sealed class PetaKatUnit : UnitTypes
        {
            public PetaKatUnit() : base(22, "PetaKat", "1000000000000000000000000000") { }
        }
        
        private sealed class FatKatUnit : UnitTypes
        {
            public FatKatUnit() : base(23, "FatKat", "1000000000000000000000000000000") { }
        }
    }
}


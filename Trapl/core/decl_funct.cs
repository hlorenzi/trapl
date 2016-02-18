using System.Collections.Generic;


namespace Trapl.Core
{
    public class DeclFunct
    {
        public class LocalBinding
        {
            public Name name;
            public int registerIndex;
        }


        public List<Type> registerTypes = new List<Type>();
        public int parameterNum;
        public List<LocalBinding> localBindings = new List<LocalBinding>();
        public List<InstructionSegment> segments = new List<InstructionSegment>();


        public int CreateSegment()
        {
            this.segments.Add(new InstructionSegment());
            return this.segments.Count - 1;
        }


        public void AddInstruction(int segmentIndex, Instruction inst)
        {
            this.segments[segmentIndex].instructions.Add(inst);
        }


        public int CreateRegister(Core.Type type)
        {
            this.registerTypes.Add(type);
            return this.registerTypes.Count - 1;
        }


        public int CreateBinding(Core.Name name, int registerIndex)
        {
            var binding = new LocalBinding();
            binding.name = name;
            binding.registerIndex = registerIndex;
            this.localBindings.Add(binding);
            return this.localBindings.Count - 1;
        }


        public Core.TypeFunct MakeFunctType()
        {
            var parameterTypes = new Core.Type[this.parameterNum];
            for (var i = 0; i < this.parameterNum; i++)
                parameterTypes[i] = this.registerTypes[i + 1];

            return Core.TypeFunct.Of(this.registerTypes[0], parameterTypes);
        }
    }
}

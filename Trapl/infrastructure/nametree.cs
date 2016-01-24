using System.Collections.Generic;


namespace Trapl.Infrastructure
{
    public class NameTree<T>
    {
        private class IdentifierGroup
        {
            public Dictionary<string, IdentifierGroup> innerGroups = new Dictionary<string, IdentifierGroup>();
            public bool hasValue;
            public T value;
        }


        private IdentifierGroup globalGroup = new IdentifierGroup();


        public void Add(T value, Name name)
        {
            if (name.identifiers.Length == 0)
                throw new System.ArgumentException("empty name");

            var curIdentifier = 0;
            var curGroup = globalGroup;
            while (curIdentifier < name.identifiers.Length)
            {
                IdentifierGroup nextGroup;
                if (!curGroup.innerGroups.TryGetValue(name.identifiers[curIdentifier], out nextGroup))
                {
                    nextGroup = new IdentifierGroup();
                    curGroup.innerGroups.Add(name.identifiers[curIdentifier], nextGroup);
                }

                curGroup = nextGroup;
                curIdentifier++;
            }

            if (curGroup.hasValue)
                throw new System.ArgumentException("duplicate name");

            curGroup.hasValue = true;
            curGroup.value = value;
        }


        public bool FindByName(out T value, Name name)
        {
            value = default(T);

            if (name.identifiers.Length == 0)
                return false;

            var curIdentifier = 0;
            var curGroup = globalGroup;
            while (curIdentifier < name.identifiers.Length)
            {
                IdentifierGroup nextGroup;
                if (!curGroup.innerGroups.TryGetValue(name.identifiers[curIdentifier], out nextGroup))
                    return false;

                curGroup = nextGroup;
                curIdentifier++;
            }

            value = curGroup.value;
            return curGroup.hasValue;
        }


        public bool FindByValue(T value, out Name identifierPath)
        {
            identifierPath = null;

            foreach (var pair in this.EnumerateGroup(new List<string>(), this.globalGroup))
            {
                if (pair.Item2.Equals(value))
                {
                    identifierPath = pair.Item1;
                    return true;
                }
            }

            return false;
        }


        public IEnumerable<System.Tuple<Name, T>> Enumerate()
        {
            foreach (var pair in this.EnumerateGroup(new List<string>(), this.globalGroup))
                yield return pair;
        }


        private IEnumerable<System.Tuple<Name, T>> EnumerateGroup(List<string> path, IdentifierGroup group)
        {
            if (group.hasValue)
                yield return new System.Tuple<Name, T>(Name.FromPath(path.ToArray()), group.value);

            foreach (var innerGroup in group.innerGroups)
            {
                path.Add(innerGroup.Key);
                foreach (var value in this.EnumerateGroup(path, innerGroup.Value))
                    yield return value;
                path.RemoveAt(path.Count - 1);
            }
        }
    }
}

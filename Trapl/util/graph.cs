using System.Collections.Generic;


namespace Trapl.Util
{
    public class Graph<T>
    {
        List<T> nodes = new List<T>();
        Dictionary<T, List<T>> outgoingEdges = new Dictionary<T, List<T>>();


        public Graph()
        {

        }


        public void AddNode(T item)
        {
            this.nodes.Add(item);
        }


        public void AddEdge(T from, T to)
        {
            List<T> outgoingList;
            if (!this.outgoingEdges.TryGetValue(from, out outgoingList))
            {
                outgoingList = new List<T>();
                this.outgoingEdges.Add(from, outgoingList);
            }

            outgoingList.Add(to);
        }


        public List<T> GetOutgoingEdges(T node)
        {
            List<T> outgoingList;
            if (!this.outgoingEdges.TryGetValue(node, out outgoingList))
                return new List<T>();

            return outgoingList;
        }


        public List<T> GetTopologicalSort()
        {
            var sortedNodes = new List<T>();
            var visitedNodes = new HashSet<T>();

            foreach (var node in this.nodes)
            {
                if (!this.VisitForTopologicalSort(node, visitedNodes, sortedNodes))
                    return null;
            }

            return sortedNodes;
        }


        private bool VisitForTopologicalSort(T node, HashSet<T> visitedNodes, List<T> sortedNodes)
        {
            if (!visitedNodes.Contains(node))
            {
                visitedNodes.Add(node);

                foreach (var dep in this.GetOutgoingEdges(node))
                    VisitForTopologicalSort(dep, visitedNodes, sortedNodes);

                sortedNodes.Add(node);
                return true;
            }
            else
            {
                return sortedNodes.Contains(node);
            }
        }
    }
}

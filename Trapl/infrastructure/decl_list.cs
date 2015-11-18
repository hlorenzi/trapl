using System.Collections.Generic;


namespace Trapl.Infrastructure
{
    public class DeclList<T>
    {
        public List<List<T>> contentList = new List<List<T>>();
        public List<Grammar.ASTNode> pathList = new List<Grammar.ASTNode>();


        public void Add(Grammar.ASTNode pathASTNode, T contents)
        {
            if (pathASTNode.kind != Grammar.ASTNodeKind.Path)
                throw new InternalException("node is not a Path");

            var pathIndex = pathList.FindIndex(p => Semantics.PathUtil.Compare(p, pathASTNode));
            if (pathIndex < 0)
            {
                pathList.Add(pathASTNode);
                var list = new List<T>();
                list.Add(contents);
                contentList.Add(list);
            }
            else
            {
                contentList[pathIndex].Add(contents);
            }
        }


        public List<T> GetDeclsClone(Grammar.ASTNode pathASTNode)
        {
            if (pathASTNode.kind != Grammar.ASTNodeKind.Path)
                throw new InternalException("node is not a Path");

            var pathIndex = pathList.FindIndex(p => Semantics.PathUtil.Compare(p, pathASTNode));
            if (pathIndex < 0)
                return new List<T>();

            return new List<T>(contentList[pathIndex]);
        }


        public void ForEach(System.Action<T> func)
        {
            var listListCopy = new List<List<T>>();
            foreach (var contents in contentList)
            {
                var listCopy = new List<T>();
                foreach (var content in contents)
                    listCopy.Add(content);
                listListCopy.Add(listCopy);
            }

            foreach (var contents in listListCopy)
            {
                foreach (var content in contents)
                    func(content);
            }
        }


        public IEnumerable<T> Enumerate()
        {
            foreach (var contents in contentList)
            {
                foreach (var content in contents)
                    yield return content;
            }
        }
    }
}

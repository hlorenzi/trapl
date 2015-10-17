﻿using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public static class LocalInitializationCheck
    {
        public static void Check(Interface.Session session)
        {
            foreach (var topDecl in session.topDecls)
            {
                if (!topDecl.bodyResolved || topDecl.primitive)
                    continue;

                var def = (topDecl.def as DefFunct);
                if (def == null)
                    continue;

                CheckFunct(session, def);
            }
        }


        private static void CheckFunct(Interface.Session session, DefFunct fn)
        {
            var initStatus = new List<bool>();
            for (var i = 0; i < fn.localVariables.Count; i++)
                initStatus.Add(i < fn.arguments.Count ? true : false);

            var segmentPath = new Stack<CodeSegment>();
            segmentPath.Push(fn.body);

            CheckSegment(session, fn, initStatus, fn.body, segmentPath);
        }


        private static void CheckSegment(Interface.Session session, DefFunct fn, List<bool> initStatus, CodeSegment seg, Stack<CodeSegment> segPath)
        {
            foreach (var code in seg.nodes)
            {
                var localInit = (code as CodeNodeLocalInit);
                var pushLocal = (code as CodeNodePushLocal);
                
                if (localInit != null)
                {
                    var localIndex = localInit.localIndex;
                    initStatus[localIndex] = true;
                }
                else if (pushLocal != null && !pushLocal.asReference)
                {
                    var localIndex = pushLocal.localIndex;
                    if (!initStatus[localIndex])
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UninitializedLocal,
                            "use of possibly uninitialized local '" + fn.localVariables[localIndex].name + "'",
                            code.span);

                        initStatus[localIndex] = true;
                    }
                }
            }

            segPath.Push(seg);
            foreach (var nextSeg in seg.outwardPaths)
            {
                if (!segPath.Contains(nextSeg))
                {
                    var initStatusClone = new List<bool>(initStatus);
                    CheckSegment(session, fn, initStatusClone, nextSeg, segPath);
                }
            }
            segPath.Pop();
        }
    }
}

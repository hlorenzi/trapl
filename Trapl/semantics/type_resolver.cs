using System.Collections.Generic;


namespace Trapl.Semantics
{
    public static class TypeResolver
    {
        public static Core.Type Resolve(
            Core.Session session,
            Grammar.ASTNodeType typeNode,
            IList<Core.UseDirective> useDirectives,
            bool mustBeResolved)
        {
            var typePlaceholderNode = typeNode as Grammar.ASTNodeTypePlaceholder;
            if (typePlaceholderNode != null)
            {
                if (mustBeResolved)
                {
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.Expected,
                        "type must be known",
                        typeNode.GetSpan());
                    return new Core.TypeError();
                }
                
                return new Core.TypePlaceholder();
            }

            var typeStructNode = typeNode as Grammar.ASTNodeTypeStruct;
            if (typeStructNode != null)
            {
                var name = NameResolver.Resolve(typeStructNode.name);

                var foundDecls = session.GetDeclsWithUseDirectives(name, typeStructNode.name.path.isRooted, useDirectives);
                if (!session.ValidateSingleDecl(foundDecls, name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                if (!session.ValidateAsType(foundDecls[0], name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                return Core.TypeStruct.Of(foundDecls[0].index);
            }

            var typeTupleNode = typeNode as Grammar.ASTNodeTypeTuple;
            if (typeTupleNode != null)
            {
                var elementTypes = new Core.Type[typeTupleNode.elements.Count];
                for (var i = 0; i < typeTupleNode.elements.Count; i++)
                    elementTypes[i] = Resolve(session, typeTupleNode.elements[i], useDirectives, mustBeResolved);

                return Core.TypeTuple.Of(elementTypes);
            }

            throw new System.NotImplementedException();
        }


        public static void ValidateDataAccess(
            Core.Session session,
            Core.DeclFunct funct,
            Core.DataAccess access)
        {
            var regAccess = access as Core.DataAccessRegister;
            if (regAccess != null)
            {
                var type = GetFinalFieldType(
                    session,
                    funct,
                    funct.registerTypes[regAccess.registerIndex],
                    regAccess.fieldAccesses);

                if (regAccess.dereference)
                {
                    var typePtr = type as Core.TypePointer;
                    if (typePtr == null)
                    {
                        session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.CannotDereferenceType,
                            "dereferencing '" + type.GetString(session) + "'",
                            access.span);
                    }
                }
            }
        }


        public static Core.Type GetDataAccessType(
            Core.Session session,
            Core.DeclFunct funct,
            Core.DataAccess access)
        {
            var regAccess = access as Core.DataAccessRegister;
            if (regAccess != null)
            {
                var type = GetFinalFieldType(
                    session,
                    funct,
                    funct.registerTypes[regAccess.registerIndex],
                    regAccess.fieldAccesses);

                if (regAccess.dereference)
                {
                    var typePtr = type as Core.TypePointer;
                    if (typePtr == null)
                        return new Core.TypeError();

                    return typePtr.pointedToType;
                }

                return type;
            }

            return new Core.TypeError();
        }


        public static int GetFieldNum(
            Core.Session session,
            Core.DeclFunct funct,
            Core.Type baseType)
        {
            var baseStruct = baseType as Core.TypeStruct;
            if (baseStruct != null)
                return session.GetStruct(baseStruct.structIndex).fieldTypes.Count;

            var baseTuple = baseType as Core.TypeTuple;
            if (baseTuple != null)
                return baseTuple.elementTypes.Length;

            return 0;
        }


        public static Core.Type GetFieldType(
            Core.Session session,
            Core.DeclFunct funct,
            Core.Type baseType,
            int fieldIndex)
        {
            var baseStruct = baseType as Core.TypeStruct;
            if (baseStruct != null)
                return session.GetStruct(baseStruct.structIndex).fieldTypes[fieldIndex];

            var baseTuple = baseType as Core.TypeTuple;
            if (baseTuple != null)
                return baseTuple.elementTypes[fieldIndex];

            return new Core.TypeError();
        }


        public static Core.Type GetFinalFieldType(
            Core.Session session,
            Core.DeclFunct funct,
            Core.Type baseType,
            Core.FieldAccesses fields)
        {
            var curType = baseType;

            for (var i = 0; i < fields.indices.Count; i++)
                curType = GetFieldType(session, funct, curType, fields.indices[i]);

            return curType;
        }
    }
}

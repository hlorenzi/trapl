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
                return ResolveStruct(session, typeStructNode.name, useDirectives, mustBeResolved);
            }

            var typeTupleNode = typeNode as Grammar.ASTNodeTypeTuple;
            if (typeTupleNode != null)
            {
                var elementTypes = new Core.Type[typeTupleNode.elements.Count];
                for (var i = 0; i < typeTupleNode.elements.Count; i++)
                    elementTypes[i] = Resolve(session, typeTupleNode.elements[i], useDirectives, mustBeResolved);

                return Core.TypeTuple.Of(elementTypes);
            }

            var typeFunctNode = typeNode as Grammar.ASTNodeTypeFunct;
            if (typeFunctNode != null)
            {
                var returnType = Resolve(session, typeFunctNode.returnType, useDirectives, mustBeResolved);
                var parameterTypes = new Core.Type[typeFunctNode.parameters.Count];
                for (var i = 0; i < typeFunctNode.parameters.Count; i++)
                    parameterTypes[i] = Resolve(session, typeFunctNode.parameters[i], useDirectives, mustBeResolved);

                return Core.TypeFunct.Of(returnType, parameterTypes);
            }

            var typeRefNode = typeNode as Grammar.ASTNodeTypePointer;
            if (typeRefNode != null) 
            {
                return Core.TypePointer.Of(
                    typeRefNode.mutable,
                    Resolve(session, typeRefNode.referenced, useDirectives, mustBeResolved));
            }

            throw new System.NotImplementedException();
        }


        public static Core.Type ResolveStruct(
            Core.Session session,
            Grammar.ASTNodeName nameNode,
            IList<Core.UseDirective> useDirectives,
            bool mustBeResolved)
        {
            var name = NameResolver.Resolve(nameNode);

            var foundDecls = session.GetDeclsWithUseDirectives(name, nameNode.path.isRooted, useDirectives);
            if (!session.ValidateSingleDecl(foundDecls, name, nameNode.GetSpan()))
                return new Core.TypeError();

            if (!session.ValidateAsType(foundDecls[0], name, nameNode.GetSpan()))
                return new Core.TypeError();

            return Core.TypeStruct.Of(foundDecls[0].index);
        }


        public static Core.Type ResolveStruct(
            Core.Session session,
            Grammar.ASTNodeExprName nameNode,
            IList<Core.UseDirective> useDirectives,
            bool mustBeResolved)
        {
            var nameConcreteNode = nameNode as Grammar.ASTNodeExprNameConcrete;
            if (nameConcreteNode == null)
            {
                if (mustBeResolved)
                {
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.Expected,
                        "type must be known",
                        nameNode.GetSpan());
                }

                return new Core.TypeError();
            }

            return ResolveStruct(session, nameConcreteNode.name, useDirectives, mustBeResolved);
        }


        public static bool ValidateDataAccess(
            Core.Session session,
            Core.DeclFunct funct,
            Core.DataAccess access)
        {
            var regDeref = access as Core.DataAccessDereference;
            if (regDeref != null)
            {
                var innerType = GetDataAccessType(session, funct, regDeref.innerAccess);
                var innerPtr = innerType as Core.TypePointer;
                if (innerPtr == null)
                {
                    session.AddMessage(
                        Diagnostics.MessageKind.Error,
                        Diagnostics.MessageCode.CannotDereferenceType,
                        "dereferencing '" + innerType.GetString(session) + "'",
                        access.span);
                    return false;
                }
            }
            return true;
        }


        public static Core.Type GetDataAccessType(
            Core.Session session,
            Core.DeclFunct funct,
            Core.DataAccess access)
        {
            var regAccess = access as Core.DataAccessRegister;
            if (regAccess != null)
            {
                return funct.registerTypes[regAccess.registerIndex];
            }

            var regField = access as Core.DataAccessField;
            if (regField != null)
            {
                return GetFieldType(
                    session, GetDataAccessType(session, funct, regField.baseAccess), regField.fieldIndex);
            }

            var regDeref = access as Core.DataAccessDereference;
            if (regDeref != null)
            {
                var innerType = GetDataAccessType(session, funct, regDeref.innerAccess);
                var innerPtr = innerType as Core.TypePointer;
                if (innerPtr == null)
                    return new Core.TypeError();

                return innerPtr.pointedToType;
            }

            var regDiscard = access as Core.DataAccessDiscard;
            if (regDiscard != null)
            {
                return Core.TypeTuple.Empty();
            }

            return new Core.TypeError();
        }


        public static int GetFieldNum(
            Core.Session session,
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


        public static Core.Name GetFieldName(
            Core.Session session,
            Core.Type baseType,
            int fieldIndex)
        {
            var baseStruct = baseType as Core.TypeStruct;
            if (baseStruct != null)
            {
                Core.Name name;
                session.GetStruct(baseStruct.structIndex).fieldNames.FindByValue(fieldIndex, out name);
                return name;
            }

            var baseTuple = baseType as Core.TypeTuple;
            if (baseTuple != null)
                return Core.Name.FromPath(fieldIndex.ToString());

            return null;
        }


        public static bool GetDataAccessMutability(
            Core.Session session,
            Core.DeclFunct funct,
            Core.DataAccess access)
        {
            var regAccess = access as Core.DataAccessRegister;
            if (regAccess != null)
            {
                return funct.registerMutabilities[regAccess.registerIndex];
            }

            var regField = access as Core.DataAccessField;
            if (regField != null)
            {
                return GetDataAccessMutability(session, funct, regField.baseAccess);
            }

            var regDeref = access as Core.DataAccessDereference;
            if (regDeref != null)
            {
                var innerType = GetDataAccessType(session, funct, regDeref.innerAccess);
                var innerPtr = innerType as Core.TypePointer;
                if (innerPtr == null)
                    return false;

                if (!innerPtr.mutable)
                    return false;

                return true;
            }

            return false;
        }
    }
}

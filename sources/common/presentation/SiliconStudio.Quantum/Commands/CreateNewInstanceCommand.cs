﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Quantum.Commands
{
    public class CreateNewInstanceCommand : ChangeValueCommand
    {
        private static readonly object SetToNullObject = new object();

        /// <summary>
        /// An object that can be passed as parameter to the command, in order to set the value of the node to <c>null</c>.
        /// </summary>
        public static object SetToNull { get { return SetToNullObject; } }

        /// <inheritdoc/>
        public override string Name { get { return "CreateNewInstance"; } }

        /// <inheritdoc/>
        public override CombineMode CombineMode { get { return CombineMode.CombineOnlyForAll; } }

        /// <inheritdoc/>
        public override bool CanAttach(ITypeDescriptor typeDescriptor, MemberDescriptorBase memberDescriptor)
        {
            var type = typeDescriptor.GetInnerCollectionType();
            var isNullableStruct = type.IsNullable() && Nullable.GetUnderlyingType(type).IsStruct();
            var isAbstractOrClass = type.IsAbstract || type.IsClass;
            //var isCollection = (typeDescriptor is CollectionDescriptor) || (typeDescriptor is DictionaryDescriptor);

            //var result = isNullableStruct || (isAbstractOrClass && !isCollection);
            var result = isNullableStruct || isAbstractOrClass;
            return result;
        }

        protected override object ChangeValue(object currentValue, object parameter, bool isRedo)
        {
            if (parameter == SetToNull)
                return null;

            var type = parameter as Type;
            return type != null && (currentValue == null || currentValue.GetType() != type) ? ObjectFactory.NewInstance(type) : currentValue;
        }
    }
}

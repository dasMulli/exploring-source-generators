﻿//HintName: AutoGenerateAttribute.gen.cs

using System;
namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [System.Diagnostics.Conditional("PropertyChangedGenerator_DEBUG")]
    sealed class AutoNotifyAttribute : Attribute
    {
        private readonly Type propertyInterfaceType;

        public AutoNotifyAttribute(Type propertyInterfaceType)
        {
            this.propertyInterfaceType = propertyInterfaceType;
        }

        public Type PropertyInterfaceType
        {
            get { return this.propertyInterfaceType; }
        }
    }
}
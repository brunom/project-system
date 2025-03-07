﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.VisualBasic
{
    /// <summary>
    ///     Provides the Visual Basic implementation of <see cref="IItemTypeGuidProvider"/>.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapabilities.VB)]
    internal class VisualBasicProjectTypeGuidProvider : IItemTypeGuidProvider
    {
        [ImportingConstructor]
        public VisualBasicProjectTypeGuidProvider()
        {
        }

        public Guid ProjectTypeGuid
        {
            get { return ProjectType.LegacyVisualBasicGuid; }
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Returns the set of Startup objects (or entry point types) in a project.
    /// </summary>
    [ExportDynamicEnumValuesProvider("StartupObjectsEnumProvider")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class StartupObjectsEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly Workspace _workspace;
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public StartupObjectsEnumProvider([Import(typeof(VisualStudioWorkspace))] Workspace workspace, UnconfiguredProject project)
        {
            _workspace = workspace;
            _unconfiguredProject = project;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            // We only include a value representing the "not set" state if requested. This is
            // because the old property pages explicitly add the "(Not set)" value at the UI
            // layer; the new property pages do not have that option and so the value must come
            // from the enum provider.
            // When this project system no longer needs to support the old property pages we can
            // remove this and always include the "(Not set)" value.
            bool includeEmptyValue = options?.Any(pair =>
                pair.Name == "IncludeEmptyValue"
                && bool.TryParse(pair.Value, out bool optionValue)
                && optionValue) ?? false;

            return Task.FromResult<IDynamicEnumValuesGenerator>(new StartupObjectsEnumGenerator(_workspace, _unconfiguredProject, includeEmptyValue));
        }
    }

    internal class StartupObjectsEnumGenerator : IDynamicEnumValuesGenerator
    {
        public bool AllowCustomValues => true;
        private readonly Workspace _workspace;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly bool _includeEmptyValue;

        /// <summary>
        /// When we implement WinForms support, we need to set this for VB WinForms projects
        /// </summary>
        private static bool SearchForEntryPointsInFormsOnly => false;

        [ImportingConstructor]
        public StartupObjectsEnumGenerator(Workspace workspace, UnconfiguredProject project, bool includeEmptyValue)
        {
            _workspace = workspace;
            _unconfiguredProject = project;
            _includeEmptyValue = includeEmptyValue;
        }

        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            Project? project = _workspace.CurrentSolution.Projects.FirstOrDefault(p => PathHelper.IsSamePath(p.FilePath!, _unconfiguredProject.FullPath));
            if (project is null)
            {
                return Array.Empty<IEnumValue>();
            }

            List<IEnumValue> enumValues = new();

            Compilation? compilation = await project.GetCompilationAsync();

            if (_includeEmptyValue)
            {
                enumValues.Add(new PageEnumValue(new EnumValue { Name = "", DisplayName = VSResources.StartupObjectNotSet }));
            }

            IEntryPointFinderService? entryPointFinderService = project.LanguageServices.GetService<IEntryPointFinderService>();
            IEnumerable<INamedTypeSymbol>? entryPoints = entryPointFinderService?.FindEntryPoints(compilation?.GlobalNamespace, SearchForEntryPointsInFormsOnly);
            if (entryPoints is not null)
            {
                enumValues.AddRange(entryPoints.Select(ep =>
                {
                    string name = ep.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return new PageEnumValue(new EnumValue { Name = name, DisplayName = name });
                }));
            }

            return enumValues;
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
            return Task.FromResult<IEnumValue?>(value);
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    public abstract class BuildUpToDateCheckTestBase
    {
        private static readonly IImmutableList<IItemType> _itemTypes = ImmutableList<IItemType>.Empty
            .Add(new ItemType("None",             upToDateCheckInput: true))
            .Add(new ItemType("Content",          upToDateCheckInput: true))
            .Add(new ItemType("Compile",          upToDateCheckInput: true))
            .Add(new ItemType("Resource",         upToDateCheckInput: true))
            .Add(new ItemType("EmbeddedResource", upToDateCheckInput: true));

        private protected static IProjectRuleSnapshotModel SimpleItems(params string[] items)
        {
            return new IProjectRuleSnapshotModel
            {
                Items = items.Aggregate(
                    ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal,
                    (current, item) => current.Add(item, ImmutableStringDictionary<string>.EmptyOrdinal))
            };
        }

        private protected static IProjectRuleSnapshotModel ItemWithMetadata(string itemSpec, string metadataName, string metadataValue)
        {
            return new IProjectRuleSnapshotModel
            {
                Items = ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal.Add(
                    itemSpec,
                    ImmutableStringDictionary<string>.EmptyOrdinal.Add(metadataName, metadataValue))
            };
        }

        private protected static IProjectRuleSnapshotModel ItemWithMetadata(string itemSpec, params (string MetadataName, string MetadataValue)[] metadata)
        {
            return new IProjectRuleSnapshotModel
            {
                Items = ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal.Add(
                    itemSpec,
                    metadata.ToImmutableDictionary(pair => pair.MetadataName, pair => pair.MetadataValue, StringComparer.Ordinal))
            };
        }

        private protected static IProjectRuleSnapshotModel ItemsWithMetadata(params (string itemSpec, string metadataName, string metadataValue)[] items)
        {
            return new IProjectRuleSnapshotModel
            {
                Items = ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal.AddRange(
                    items.Select(
                        item => new KeyValuePair<string, IImmutableDictionary<string, string>>(
                            item.itemSpec,
                            ImmutableStringDictionary<string>.EmptyOrdinal.Add(item.metadataName, item.metadataValue))))
            };
        }

        private protected static IProjectRuleSnapshotModel Union(params IProjectRuleSnapshotModel[] models)
        {
            var items = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;

            foreach (var model in models)
            {
                foreach ((string key, IImmutableDictionary<string, string> value) in model.Items)
                {
                    items = items.Add(key, value);
                }
            }

            return new IProjectRuleSnapshotModel { Items = items };
        }

        private protected static UpToDateCheckImplicitConfiguredInput UpdateState(
            UpToDateCheckImplicitConfiguredInput priorState,
            Dictionary<string, IProjectRuleSnapshotModel>? projectRuleSnapshot = null,
            Dictionary<string, IProjectRuleSnapshotModel>? sourceRuleSnapshot = null,
            bool itemRemovedFromSourceSnapshot = false)
        {
            return priorState.Update(
                CreateUpdate(projectRuleSnapshot),
                CreateUpdate(sourceRuleSnapshot, itemRemovedFromSourceSnapshot),
                IProjectItemSchemaFactory.Create(_itemTypes),
                IProjectCatalogSnapshotFactory.CreateWithDefaultMapping(_itemTypes));

            static IProjectSubscriptionUpdate CreateUpdate(Dictionary<string, IProjectRuleSnapshotModel>? snapshotBySchemaName, bool itemRemovedFromSnapshot = false)
            {
                var snapshots = ImmutableStringDictionary<IProjectRuleSnapshot>.EmptyOrdinal;
                var changes = ImmutableStringDictionary<IProjectChangeDescription>.EmptyOrdinal;

                if (snapshotBySchemaName != null)
                {
                    foreach ((string schemaName, IProjectRuleSnapshotModel model) in snapshotBySchemaName)
                    {
                        var change = itemRemovedFromSnapshot
                            ? new IProjectChangeDescriptionModel
                            {
                                Before = model,
                                After = new IProjectRuleSnapshotModel(),
                                Difference = new IProjectChangeDiffModel { AnyChanges = true, RemovedItems = model.Items.Select(a => a.Key).ToImmutableHashSet() }
                            }
                            : new IProjectChangeDescriptionModel
                            {
                                Before = new IProjectRuleSnapshotModel(),
                                After = model,
                                Difference = new IProjectChangeDiffModel { AnyChanges = true }
                            };

                        snapshots = snapshots.Add(schemaName, model.ToActualModel());
                        changes = changes.Add(schemaName, change.ToActualModel());
                    }
                }

                return IProjectSubscriptionUpdateFactory.Implement(snapshots, changes);
            }
        }
    }
}

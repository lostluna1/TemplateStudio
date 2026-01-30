// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.PostActions.Catalog;
using Microsoft.Templates.Core.PostActions.Catalog.Merge;
using Microsoft.Templates.Core.PostActions.Catalog.SortNamespaces;

namespace Microsoft.Templates.Core.PostActions
{
    public class NewItemPostActionFactory : PostActionFactory
    {
        // Store pending navigation item moves to be processed after sync
        // Using a HashSet to avoid duplicates
        private static readonly HashSet<(string ItemId, string ParentNavigationId)> _pendingNavigationMoves
            = new HashSet<(string ItemId, string ParentNavigationId)>();

        public override IEnumerable<PostAction> FindPostActions(GenInfo genInfo, ITemplateCreationResult genResult)
        {
            var postActions = new List<PostAction>();

            AddPredefinedActions(genInfo, genResult, postActions);
            AddTemplateDefinedPostActions(genInfo, genResult, postActions);
            AddGetMergeFilesFromProjectPostAction(genInfo, postActions);
            AddGenerateMergeInfoPostAction(genInfo, postActions);
            AddMergeActions(genInfo, postActions, $"*{MergeConfiguration.Extension}*", false);
            AddSearchAndReplaceActions(genInfo, postActions, $"*{MergeConfiguration.SearchReplaceExtension}*", false);

            // Store navigation item info for later processing (after files are copied to project)
            StoreNavigationItemInfo(genInfo);

            return postActions;
        }

        private void StoreNavigationItemInfo(GenInfo genInfo)
        {
            // Check if we have a ParentNavigationId parameter
            if (genInfo.Parameters.TryGetValue(GenParams.ParentNavigationId, out var parentNavigationId)
                && !string.IsNullOrEmpty(parentNavigationId))
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] Storing navigation move: ItemId={genInfo.Name}, ParentId={parentNavigationId}");
                _pendingNavigationMoves.Add((genInfo.Name, parentNavigationId));
            }
        }

        public override IEnumerable<PostAction> FindGlobalPostActions()
        {
            var postActions = new List<PostAction>();

            AddGlobalMergeActions(postActions, $"*{MergeConfiguration.GlobalExtension}*", false);

            var paths = new List<string>() { Path.GetDirectoryName(GenContext.Current.GenerationOutputPath) };

            postActions.Add(new SortUsingsPostAction(paths));
            postActions.Add(new SortImportsPostAction(paths));

            return postActions;
        }

        public override IEnumerable<PostAction> FindSyncGenerationPostActions(TempGenerationResult result)
        {
            var postActions = new List<PostAction>();

            postActions.Add(new CopyFilesToProjectPostAction(result));
            postActions.Add(new AddContextItemsToSolutionAndProjectPostAction());

            // Process pending navigation item moves AFTER files are copied to project
            AddPendingNavigationMovePostActions(postActions);

            postActions.Add(new CreateSummaryPostAction(result));
            postActions.Add(new OpenFilesPostAction());

            return postActions;
        }

        private void AddPendingNavigationMovePostActions(List<PostAction> postActions)
        {
            if (_pendingNavigationMoves.Count == 0)
            {
                return;
            }

            var projectPath = GenContext.Current.DestinationPath;
            var navigationConfigPath = Path.Combine(projectPath, "Services", "NavigationConfig.xml");

            System.Diagnostics.Debug.WriteLine($"[TabsNavView] Processing {_pendingNavigationMoves.Count} pending navigation moves");
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] NavigationConfig path: {navigationConfigPath}");

            // Take a copy and clear immediately to avoid duplicates on re-entry
            var moves = _pendingNavigationMoves.ToList();
            _pendingNavigationMoves.Clear();

            foreach (var (itemId, parentNavigationId) in moves)
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] Adding MoveNavigationItemToParentPostAction: Item={itemId}, Parent={parentNavigationId}");
                postActions.Add(new MoveNavigationItemToParentPostAction(
                    "TabsNavView",
                    navigationConfigPath,
                    itemId,
                    parentNavigationId));
            }
        }

        public override IEnumerable<PostAction> FindOutputGenerationPostActions(TempGenerationResult result)
        {
            var postActions = new List<PostAction>();

            postActions.Add(new CreateSummaryPostAction(result));
            postActions.Add(new OpenFilesPostAction());

            return postActions;
        }
    }
}

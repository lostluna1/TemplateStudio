// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Diagnostics;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.SharedResources;
using Microsoft.Templates.UI.Controls;
using Microsoft.Templates.UI.Mvvm;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.Threading;
using Microsoft.Templates.UI.ViewModels.Common;
using Microsoft.Templates.UI.Views.NewItem;

namespace Microsoft.Templates.UI.ViewModels.NewItem
{
    public class MainViewModel : BaseMainViewModel
    {
        public const string NewItemStepTemplateSelection = "TemplateSelection";
        public const string NewItemStepParentNavigation = "ParentNavigation";
        public const string NewItemStepChangesSummary = "ChangesSummary";

        private readonly GenerationService _generationService = GenerationService.Instance;

        private RelayCommand _refreshTemplatesCacheCommand;

        private static NewItemGenerationResult _output;

        private bool _isTabsNavViewProject;

        public TemplateType TemplateType { get; set; }

        public static MainViewModel Instance { get; private set; }

        public TemplateSelectionViewModel TemplateSelection { get; } = new TemplateSelectionViewModel();

        public ParentNavigationSelectionViewModel ParentNavigationSelection { get; } = new ParentNavigationSelectionViewModel();

        public ObservableCollection<BreakingChangeMessageViewModel> BreakingChangesErrors { get; set; } = new ObservableCollection<BreakingChangeMessageViewModel>();

        public ChangesSummaryViewModel ChangesSummary { get; } = new ChangesSummaryViewModel();

        public RelayCommand RefreshTemplatesCacheCommand => _refreshTemplatesCacheCommand ?? (_refreshTemplatesCacheCommand = new RelayCommand(
            () => SafeThreading.JoinableTaskFactory.RunAsync(async () => await OnRefreshTemplatesAsync())));

        public Visibility RefreshTemplateCacheVisibility
        {
            get
            {
#if DEBUG
                return Visibility.Visible;
#else
                return Visibility.Collapsed;
#endif
            }
        }

        public MainViewModel(NewItemWizardShell mainWindow, BaseStyleValuesProvider provider)
            : base(mainWindow, provider, NewItemSteps, false)
        {
            Instance = this;
            Navigation.OnFinish += OnFinish;
            Navigation.OnStepUpdated += OnStepUpdated;
            Navigation.IsStepAvailable = IsStepAvailableAsync;
        }

        public override void UnsubscribeEventHandlers()
        {
            base.UnsubscribeEventHandlers();
            Navigation.OnFinish -= OnFinish;
            Navigation.OnStepUpdated -= OnStepUpdated;
        }

        private static IEnumerable<StepData> NewItemSteps
        {
            get
            {
                yield return StepData.MainStep(NewItemStepTemplateSelection, "1", Resources.NewItemStepOne, () => new TemplateSelectionPage(), true, true);
                yield return StepData.MainStep(NewItemStepChangesSummary, "2", Resources.NewItemStepTwo, () => new ChangesSummaryPage(_output));
            }
        }

        public void Initialize(TemplateType templateType, UserSelectionContext context)
        {
            TemplateType = templateType;
            WizardStatus.Title = GetNewItemTitle(templateType);

            // Check if this is a TabsNavView project
            _isTabsNavViewProject = context.ProjectType == "TabsNavView";

            // Add parent navigation selection step for TabsNavView projects when adding pages
            if (_isTabsNavViewProject && templateType == TemplateType.Page)
            {
                AddParentNavigationStep();
            }

            Initialize(context);
        }

        private void AddParentNavigationStep()
        {
            // Insert the parent navigation step between template selection and changes summary
            var parentNavStep = StepData.MainStep(NewItemStepParentNavigation, "2", Resources.NewItemStepParentNavigation, () => new ParentNavigationSelectionPage());

            // Find the changes summary step and update its index
            var changesSummaryStep = Navigation.Steps.FirstOrDefault(s => s.Id == NewItemStepChangesSummary);
            if (changesSummaryStep != null)
            {
                var index = Navigation.Steps.IndexOf(changesSummaryStep);
                Navigation.Steps.Insert(index, parentNavStep);

                // Update step indices
                for (int i = 0; i < Navigation.Steps.Count; i++)
                {
                    Navigation.Steps[i].Index = (i + 1).ToString();
                }
            }
        }

        private string GetNewItemTitle(TemplateType templateType)
        {
            switch (templateType)
            {
                case TemplateType.Page:
                    return Resources.NewItemTitlePage;
                case TemplateType.Feature:
                    return Resources.NewItemTitleFeature;
                case TemplateType.Service:
                    return Resources.NewItemTitleService;
                case TemplateType.Testing:
                    return Resources.NewItemTitleTesting;
                default:
                    return string.Empty;
            }
        }

        private void OnStepUpdated(object sender, StepDataEventsArgs e)
        {
            if (e.StepData.Id == NewItemStepTemplateSelection)
            {
                ChangesSummary.ClearSelected();
                WizardNavigation.Current.SetCanFinish(false);
            }
            else if (e.StepData.Id == NewItemStepParentNavigation)
            {
                // Load navigation items when entering the parent navigation step
                ParentNavigationSelection.LoadNavigationItems();
                WizardNavigation.Current.SetCanFinish(false);
            }
            else if (e.StepData.Id == NewItemStepChangesSummary)
            {
                WizardNavigation.Current.SetCanFinish(true);
            }
        }

        private async Task<bool> IsStepAvailableAsync(StepData step)
        {
            if (step.Id == NewItemStepChangesSummary && !WizardStatus.HasValidationErrors)
            {
                _output = await CleanupAndGenerateNewItemAsync();
                if (!_output.HasChangesToApply)
                {
                    var message = GetNewItemHasNoChangesMessage(TemplateType);
                    message = string.Format(message, TemplateSelection.Name);
                    var notification = Notification.Warning(message, Category.RightClickItemHasNoChanges);
                    NotificationsControl.AddNotificationAsync(notification).FireAndForget();
                }

                return _output.HasChangesToApply;
            }

            return true;
        }

        private string GetNewItemHasNoChangesMessage(TemplateType templateType)
        {
            switch (templateType)
            {
                case TemplateType.Page:
                    return Resources.NewItemHasNoChangesPage;
                case TemplateType.Feature:
                    return Resources.NewItemHasNoChangesFeature;
                case TemplateType.Service:
                    return Resources.NewItemHasNoChangesService;
                case TemplateType.Testing:
                    return Resources.NewItemHasNoChangesTesting;
                default:
                    return string.Empty;
            }
        }

        private async Task<NewItemGenerationResult> CleanupAndGenerateNewItemAsync()
        {
            NewItemGenController.Instance.CleanupTempGeneration();
            var userSelection = CreateUserSelection();
            await _generationService.GenerateNewItemAsync(TemplateSelection.Template.TemplateType, userSelection);
            return NewItemGenController.Instance.CompareOutputAndProject();
        }

        private UserSelection CreateUserSelection()
        {
            var userSelection = new UserSelection(Context) { HomeName = string.Empty };
            var selectedTemplate = new UserSelectionItem
            {
                Name = TemplateSelection.Name,
                TemplateId = TemplateSelection.Template.TemplateId,
                ParentNavigationId = _isTabsNavViewProject ? ParentNavigationSelection.GetSelectedParentId() : null
            };
            userSelection.Add(selectedTemplate, TemplateSelection.Template.TemplateType);

            foreach (var dependencyTemplate in TemplateSelection.Template.Dependencies)
            {
                var selectedTemplateDependency = new UserSelectionItem { Name = dependencyTemplate.DefaultName, TemplateId = dependencyTemplate.TemplateId };
                userSelection.Add(selectedTemplateDependency, dependencyTemplate.TemplateType);
            }

            return userSelection;
        }

        private void OnFinish(object sender, EventArgs e)
        {
            var userSelection = new UserSelection(Context);
            userSelection.Add(
                new UserSelectionItem()
                {
                    Name = TemplateSelection.Name,
                    TemplateId = TemplateSelection.Template.TemplateId,
                    ParentNavigationId = _isTabsNavViewProject ? ParentNavigationSelection.GetSelectedParentId() : null,
                }, TemplateType);
            NewItemWizardShell.Current.Result = userSelection;
            NewItemWizardShell.Current.Result.ItemGenerationType = ChangesSummary.DoNotMerge ? ItemGenerationType.Generate : ItemGenerationType.GenerateAndMerge;
        }

        public IEnumerable<string> GetNames()
        {
            return TemplateSelection.Dependencies.Select(i => i.DefaultName);
        }

        public override async Task OnTemplatesAvailableAsync()
        {
            ValidationService.Initialize(GetNames, null);
            TemplateSelection.LoadData(TemplateType, Context);
            WizardStatus.IsLoading = false;

            var result = BreakingChangesValidatorService.Validate();
            if (!result.IsValid)
            {
                var messages = result.ErrorMessages.Select(e => new BreakingChangeMessageViewModel(e));
                BreakingChangesErrors.AddRange(messages);
                OnPropertyChanged(nameof(BreakingChangesErrors));

                await Task.CompletedTask;
            }

            ValidateProjectPaths();
        }

        protected async Task OnRefreshTemplatesAsync()
        {
            try
            {
                WizardStatus.IsLoading = true;
                await GenContext.ToolBox.Repo.RefreshAsync(true);
            }
            catch (Exception ex)
            {
                await NotificationsControl.AddNotificationAsync(Notification.Error(Resources.NotificationSyncError_Refresh));

                await AppHealth.Current.Error.TrackAsync(ex.ToString());
                await AppHealth.Current.Exception.TrackAsync(ex);
            }
            finally
            {
                WizardStatus.IsLoading = GenContext.ToolBox.Repo.SyncInProgress;
            }
        }

        private void ValidateProjectPaths()
        {
            if (GenContext.Current.ProjectName != new DirectoryInfo(GenContext.Current.DestinationPath).Name)
            {
                var notification = Notification.Error(Resources.NotificationValidationError_ProjectNameAndPathDoNotMatch, ErrorCategory.ProjectPathValidation);
                NotificationsControl.AddNotificationAsync(notification).FireAndForget();
                ChangesSummary.DoNotMerge = true;
                ChangesSummary.IsDoNotMergeEnabled = false;
            }
            else
            {
                ChangesSummary.DoNotMerge = false;
                ChangesSummary.IsDoNotMergeEnabled = true;
            }
        }

        public override async Task ProcessItemAsync(object item)
        {
            if (item is TemplateInfoViewModel template)
            {
                TemplateSelection.SelectTemplate(template);
            }

            await Task.CompletedTask;
        }

        public override bool IsSelectionEnabled(MetadataType metadataType) => true;
    }
}

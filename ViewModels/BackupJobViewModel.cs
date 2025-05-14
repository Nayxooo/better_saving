using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasySave.Models; // Assuming Enums are in EasySave.Models

namespace EasySave.ViewModels
{
    public class BackupJobViewModel : INotifyPropertyChanged
    {
        private backupJob _backupJob;
        public backupJob Model { get => _backupJob; } // Expose the model

        private string _name;
        private string _sourceDirectory;
        private string _targetDirectory;
        private JobType _type;
        private JobState _state;
        private byte _progress; // Changed to byte to match Model
        private string? _errorMessage; // Made nullable to match Model

        public BackupJobViewModel(backupJob backupJob)
        {
            _backupJob = backupJob;
            // Initialize ViewModel properties from the model
            _name = _backupJob.Name;
            _sourceDirectory = _backupJob.GetSourceDirectory();
            _targetDirectory = _backupJob.GetTargetDirectory();
            _type = _backupJob.GetJobType();
            _state = _backupJob.State; // Use public property from Model
            _progress = _backupJob.Progress; // Use public property from Model
            _errorMessage = _backupJob.ErrorMessage; // Use public property from Model

            // If _backupJob implemented INotifyPropertyChanged, we would subscribe here.
            // For now, MainViewModel is responsible for updating this ViewModel's State/Progress
            // based on model changes during/after ExecuteAsync.
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    Model.Name = value; // Update the underlying model's property
                    // Consider notifying MainViewModel if job name persistence is needed immediately
                }
            }
        }

        public string SourceDirectory
        {
            get => _sourceDirectory;
            // Typically not set after creation, but if it were:
            // set { if (_sourceDirectory != value) { _sourceDirectory = value; OnPropertyChanged(); Model.SetSourceDirectory(value); } }
        }

        public string TargetDirectory
        {
            get => _targetDirectory;
            // Typically not set after creation
        }

        public JobType Type
        {
            get => _type;
            // Typically not set after creation
        }

        public JobState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                    // This setter is called by MainViewModel. The Model's state should already be
                    // updated by the business logic (ExecuteAsync). We can ensure sync if needed.
                    if (Model.State != value) Model.State = value;
                }
            }
        }

        public byte Progress // Changed to byte
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                    if (Model.Progress != value) Model.Progress = value;
                }
            }
        }

        public string? ErrorMessage // Made nullable
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    if (Model.ErrorMessage != value) Model.ErrorMessage = value;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) // string? for propertyName
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

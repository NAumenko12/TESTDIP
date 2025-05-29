using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using TESTDIP.Model;
using TESTDIP.ViewModels;

namespace TESTDIP.ViewModel
{
    public class EditSampleViewModel : INotifyPropertyChanged
    {
        private Sample _editedSample;

        public event EventHandler<bool?> RequestClose;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditSampleViewModel(Sample sampleToEdit)
        {
            EditedSample = new Sample
            {
                Id = sampleToEdit.Id,
                Metal = sampleToEdit.Metal,
                Value = sampleToEdit.Value,
                SamplingDate = sampleToEdit.SamplingDate,
                AnalyticsNumber = sampleToEdit.AnalyticsNumber,
                Type = sampleToEdit.Type,
                Fraction = sampleToEdit.Fraction,
                Repetition = sampleToEdit.Repetition
            };

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public Sample EditedSample
        {
            get => _editedSample;
            set
            {
                if (_editedSample != value)
                {
                    _editedSample = value;
                    OnPropertyChanged(nameof(EditedSample));
                }
            }
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditedSample.Value))
            {
                MessageBox.Show("Значение пробы не может быть пустым", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RequestClose?.Invoke(this, true);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

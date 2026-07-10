using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using PasswordGenerator;

namespace PasswordSecure.Application
{
    public class AppSettings : INotifyPropertyChanged
    {
        #region Variable
        private PropertyChangedEventHandler? onPropertyChanged;
        #endregion

        #region Property
        public string? LastFile
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(LastFile));
                }
            }
        }


        public double WindowWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(WindowWidth));
                }
            }
        } = 680;

        public double WindowHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(WindowHeight));
                }
            }
        } = 330;

        public GenerationSettings GenerationSettings
        {
            get;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(GenerationSettings));
                }
            }
        } = new();

        public int TimeSafePassword
        {
            get;
            set
            {
                if (field != value && value > 0)
                {
                    field = value;
                    OnPropertyChanged(nameof(TimeSafePassword));
                }
            }
        } = 30;
        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) { onPropertyChanged?.Invoke(this, e); }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => onPropertyChanged += value;
            remove => onPropertyChanged -= value;
        }
        #endregion
    }
}

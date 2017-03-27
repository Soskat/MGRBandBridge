using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BandBridge.ViewModels
{
    /// <summary>
    /// Class that implements INotifyPropertyChanged and therefore provides notification system.
    /// </summary>
    public class NotificationBase : INotifyPropertyChanged
    {
        #region Fields
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        /// <summary>
        /// Usage: SetField (Name, value); 
        /// where there is a data member
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] String property = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(property);
            return true;
        }

        /// <summary>
        /// Usage: SetField(()=> somewhere.Name = value; somewhere.Name, value)
        /// Advanced case where you rely on another property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentValue"></param>
        /// <param name="newValue"></param>
        /// <param name="DoSet"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        protected bool SetProperty<T>(T currentValue, T newValue, Action DoSet, [CallerMemberName] String property = null)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
            DoSet.Invoke();
            RaisePropertyChanged(property);
            return true;
        }

        protected void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
        #endregion
    }

    /// <summary>
    /// Class that implements INotifyPropertyChanged and therefore provides notification system.
    /// </summary>
    public class NotificationBase<T> : NotificationBase where T : class, new()
    {
        #region Fields
        protected T This;
        #endregion

        #region Operators
        public static implicit operator T(NotificationBase<T> thing) { return thing.This; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="NotificationBase"/>.
        /// </summary>
        /// <param name="thing"></param>
        public NotificationBase(T thing = null)
        {
            This = (thing == null) ? new T() : thing;
        }
        #endregion
    }
}

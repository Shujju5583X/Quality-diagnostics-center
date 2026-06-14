using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;

namespace LabSystem.UI.ViewModels
{
    public class AppointmentsViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientRepository _patientRepo;

        public ObservableCollection<Appointment> Appointments { get; } = new ObservableCollection<Appointment>();
        public ObservableCollection<Patient> Patients { get; } = new ObservableCollection<Patient>();

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                _ = LoadAppointmentsAsync();
            }
        }

        private Patient _selectedPatientForBooking;
        public Patient SelectedPatientForBooking
        {
            get => _selectedPatientForBooking;
            set { _selectedPatientForBooking = value; OnPropertyChanged(); }
        }

        private DateTime _bookingDate = DateTime.Today.AddDays(1);
        public DateTime BookingDate
        {
            get => _bookingDate;
            set { _bookingDate = value; OnPropertyChanged(); }
        }

        private string _bookingTime = "09:00";
        public string BookingTime
        {
            get => _bookingTime;
            set { _bookingTime = value; OnPropertyChanged(); }
        }

        private string _purpose;
        public string Purpose
        {
            get => _purpose;
            set { _purpose = value; OnPropertyChanged(); }
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        private int _durationMinutes = 15;
        public int DurationMinutes
        {
            get => _durationMinutes;
            set { _durationMinutes = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand BookAppointmentCommand { get; }
        public ICommand CancelAppointmentCommand { get; }
        public ICommand MarkNoShowCommand { get; }
        public ICommand MarkCompleteCommand { get; }
        public ICommand LoadPatientsCommand { get; }

        public AppointmentsViewModel(IAppointmentService appointmentService, IPatientRepository patientRepo)
        {
            _appointmentService = appointmentService;
            _patientRepo = patientRepo;

            BookAppointmentCommand = new AsyncRelayCommand(async o => await ExecuteBookAppointmentAsync());
            CancelAppointmentCommand = new AsyncRelayCommand(async o => await ExecuteCancelAppointmentAsync(o));
            MarkNoShowCommand = new AsyncRelayCommand(async o => await ExecuteMarkNoShowAsync(o));
            MarkCompleteCommand = new AsyncRelayCommand(async o => await ExecuteMarkCompleteAsync(o));
            LoadPatientsCommand = new AsyncRelayCommand(async o => await ExecuteLoadPatientsAsync());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await ExecuteLoadPatientsAsync();
            await LoadAppointmentsAsync();
        }

        public async Task LoadAppointmentsAsync()
        {
            try
            {
                var appointments = await _appointmentService.GetAppointmentsByDateAsync(_selectedDate);
                Appointments.Clear();
                foreach (var a in appointments.OrderBy(a => a.AppointmentDate))
                    Appointments.Add(a);
                StatusMessage = $"{Appointments.Count} appointment(s) on {_selectedDate:dd-MMM-yyyy}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading appointments: {ex.Message}";
            }
        }

        private async Task ExecuteLoadPatientsAsync()
        {
            try
            {
                var all = await _patientRepo.GetAllAsync();
                Patients.Clear();
                foreach (var p in all.OrderBy(p => p.FullName))
                    Patients.Add(p);
            }
            catch { }
        }

        private async Task ExecuteBookAppointmentAsync()
        {
            try
            {
                if (SelectedPatientForBooking == null) { StatusMessage = "Please select a patient."; return; }
                if (string.IsNullOrWhiteSpace(Purpose)) { StatusMessage = "Purpose is required."; return; }
                if (!TimeSpan.TryParse(BookingTime, out var time)) { StatusMessage = "Invalid time format. Use HH:mm."; return; }

                var appointmentDate = BookingDate.Date.Add(time);
                if (appointmentDate <= DateTime.Now) { StatusMessage = "Appointment must be in the future."; return; }

                var appointment = new Appointment
                {
                    PatientId = SelectedPatientForBooking.PatientId,
                    AppointmentDate = appointmentDate,
                    DurationMinutes = DurationMinutes,
                    Purpose = Purpose,
                    Notes = Notes
                };

                await _appointmentService.BookAppointmentAsync(appointment);
                StatusMessage = "Appointment booked successfully.";
                Purpose = "";
                Notes = "";
                SelectedPatientForBooking = null;
                await LoadAppointmentsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Booking failed: {ex.Message}";
            }
        }

        private async Task ExecuteCancelAppointmentAsync(object parameter)
        {
            if (parameter is Appointment appointment)
            {
                try
                {
                    await _appointmentService.CancelAppointmentAsync(appointment.AppointmentId);
                    StatusMessage = "Appointment cancelled.";
                    await LoadAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Cancel failed: {ex.Message}";
                }
            }
        }

        private async Task ExecuteMarkNoShowAsync(object parameter)
        {
            if (parameter is Appointment appointment)
            {
                try
                {
                    await _appointmentService.MarkNoShowAsync(appointment.AppointmentId);
                    StatusMessage = "Marked as no-show.";
                    await LoadAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed: {ex.Message}";
                }
            }
        }

        private async Task ExecuteMarkCompleteAsync(object parameter)
        {
            if (parameter is Appointment appointment)
            {
                try
                {
                    await _appointmentService.CompleteAsync(appointment.AppointmentId);
                    StatusMessage = "Appointment completed.";
                    await LoadAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed: {ex.Message}";
                }
            }
        }
    }
}
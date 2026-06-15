using System;
using System.Threading.Tasks;
using System.Windows;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        private async Task ExecuteSaveDoctorAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDoctorName))
            {
                MessageBox.Show("Please enter the doctor's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewDoctorPhone))
            {
                MessageBox.Show("Please enter the doctor's phone number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (SelectedDoctor == null)
                {
                    // Add new doctor
                    var doc = new Doctor
                    {
                        FullName = NewDoctorName.Trim(),
                        ContactPhone = NewDoctorPhone.Trim(),
                        Commission = NewDoctorCommission,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _doctorRepo.AddAsync(doc);
                    Log.Information("Added doctor: {DoctorName}", doc.FullName);
                    MessageBox.Show("Doctor added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Update existing doctor
                    var doc = await _doctorRepo.GetByIdAsync(SelectedDoctor.DoctorId);
                    if (doc != null)
                    {
                        doc.FullName = NewDoctorName.Trim();
                        doc.ContactPhone = NewDoctorPhone.Trim();
                        doc.Commission = NewDoctorCommission;
                        doc.UpdatedAt = DateTime.UtcNow;

                        await _doctorRepo.UpdateAsync(doc);
                        Log.Information("Updated doctor: {DoctorName} (ID: {DoctorId})", doc.FullName, doc.DoctorId);
                        MessageBox.Show("Doctor updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Reset fields & reload
                SelectedDoctor = null;
                NewDoctorName = string.Empty;
                NewDoctorPhone = string.Empty;
                NewDoctorCommission = 0;

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save doctor.");
                MessageBox.Show("Error saving doctor to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteDeleteDoctorAsync()
        {
            if (SelectedDoctor == null)
            {
                MessageBox.Show("Please select a doctor to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show($"Are you sure you want to delete Doctor '{SelectedDoctor.FullName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dialogResult == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                int docId = SelectedDoctor.DoctorId;
                await _doctorRepo.DeleteAsync(docId);
                Log.Information("Deleted doctor ID: {DoctorId}", docId);

                // Reset selection & fields
                SelectedDoctor = null;
                NewDoctorName = string.Empty;
                NewDoctorPhone = string.Empty;
                NewDoctorCommission = 0;

                await LoadDataAsync();
                MessageBox.Show("Doctor deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete doctor.");
                MessageBox.Show("Error deleting doctor from database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

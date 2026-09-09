namespace Domain12.RentalCar12;

public class RentalOrder
{
    public Car? SelectedCar { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalRent { get; set; }

    public bool Confirmed { get; set; }

    public void ToRentalCar()
    {
        throw new NotImplementedException();
    }

    public void SetRentalPeriod(DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public double CalculateRent()
    {
        throw new NotImplementedException();
    }

    public bool ConfirmRental()
    {
        throw new NotImplementedException();
    }
}

using System.ComponentModel;

namespace DAMmodels
{
    public class Location : INotifyPropertyChanged
    {
        public int SoldierId { get; }
        private double _long;
        private double _lat;
        public double Long
        {
            get { return _long; }
            set
            {
                if (_long != value)
                {
                    _long = value;
                    OnPropertyChanged(nameof(Long));
                }
            }
        }
        public double Lat
        {
            get { return _lat; }
            set
            {
                if (_lat != value)
                {
                    _lat = value;
                    OnPropertyChanged(nameof(Lat));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Move(double distanceInMeters, double angleInDegrees)
        {
            // For simplicity, moving in a straight line along the latitude
            // Each degree of latitude corresponds to roughly 111,320 meters.
            double deltaLat = distanceInMeters / 111320;
            double deltaLon = distanceInMeters / (40008000 / 360);
            Lat += deltaLat;
            Long += deltaLon;
        }
    }
}

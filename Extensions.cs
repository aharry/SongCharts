using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongCharts
{
    internal static class Extensions
    {
        public static Dictionary<Marker, BarBeat> MapMarkers(this IEnumerable<Marker> markers, IEnumerable<BarBeat> beats)
        {
            return markers.ToDictionary(
                                        item => item,
                                        item =>
                                        {
                                            var lowerValues = beats.Where(targetItem => targetItem.Time <= item.StartTime);
                                            if (lowerValues.Any())
                                            {
                                                return lowerValues.OrderByDescending(targetItem => targetItem.Time).First();
                                            }
                                            else
                                            {
                                                // Handle the case when no lower value is found
                                                return new BarBeat() { Bar = 0, Beat = 0, Time = 0 };

                                            }
                                        }
                                    );

        }

    }
}

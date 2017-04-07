using System.Collections.Generic;

namespace Dinmore.Uwp.Infrastructure.Media
{
    public static class PlayList
    {
        public static List<PlayListItem> List
        {
            get
            {
                return new List<PlayListItem>
            {
                new PlayListItem( PlayListGroup.Demographic12to17, 1, "Assets/Voice/1-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic12to17, 2, "Assets/Voice/2-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic12to17, 3, "Assets/Voice/3-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic12to17, 4, "Assets/Voice/4-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic12to17, 5, "Assets/Voice/5-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic12to17, 6, "Assets/Voice/6-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic12to17, 7, "Assets/Voice/7-12-17.wav"),

                new PlayListItem( PlayListGroup.Demographic18to24, 1, "Assets/Voice/1-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic18to24, 2, "Assets/Voice/2-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic18to24, 3, "Assets/Voice/3-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic18to24, 4, "Assets/Voice/4-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic18to24, 5, "Assets/Voice/5-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic18to24, 6, "Assets/Voice/6-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic18to24, 7, "Assets/Voice/7-12-17.wav"),

                new PlayListItem( PlayListGroup.Demographic25to34, 1, "Assets/Voice/1-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic25to34, 2, "Assets/Voice/2-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic25to34, 3, "Assets/Voice/3-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic25to34, 4, "Assets/Voice/4-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic25to34, 5, "Assets/Voice/5-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic25to34, 6, "Assets/Voice/6-12-17.wav"),
                new PlayListItem( PlayListGroup.Demographic25to34, 7, "Assets/Voice/7-12-17.wav"),

                new PlayListItem( PlayListGroup.Demographic35to44, 1, "Assets/Voice/1-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic35to44, 2, "Assets/Voice/2-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic35to44, 3, "Assets/Voice/3-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic35to44, 4, "Assets/Voice/4-55-64.wav"),

                new PlayListItem( PlayListGroup.Demographic45to54, 1, "Assets/Voice/1-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic45to54, 2, "Assets/Voice/2-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic45to54, 3, "Assets/Voice/3-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic45to54, 4, "Assets/Voice/4-55-64.wav"),

                new PlayListItem( PlayListGroup.Demographic55to64, 1, "Assets/Voice/1-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic55to64, 2, "Assets/Voice/2-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic55to64, 3, "Assets/Voice/3-55-64.wav"),
                new PlayListItem( PlayListGroup.Demographic55to64, 4, "Assets/Voice/4-55-64.wav"),

                new PlayListItem( PlayListGroup.HelloSingleFace, 1, "Assets/Voice/hello.wav"),
                new PlayListItem( PlayListGroup.HelloMultipleFace, 1, "Assets/Voice/helloeveryone.wav"),
            };
            }
        }
    }
}

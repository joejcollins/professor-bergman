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

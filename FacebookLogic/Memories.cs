using System;
using System.Collections.Generic;
using FacebookWrapper.ObjectModel;

namespace FacebookLogic
{
    internal static class Memories
    {
        internal static PostProxy.PostProxyCollection FetchPostFromThePast(User i_LoggedInUser)
        {
            IEnumerable<Post> friendsPosts = getFriendsPosts(i_LoggedInUser);
            Filter<Post> postFilterByDate = new Filter<Post>(isPostFromThisDate);
            IEnumerable<Post> postFromThePast = postFilterByDate.DoFilter(friendsPosts);
            PostProxy.PostProxyCollection proxyPostFromThePast = PostProxy.ConvertPostToPostProxyCollection(postFromThePast);

            return proxyPostFromThePast;
        }

        private static IEnumerable<Post> getFriendsPosts(User i_LoggedInUser)
        {
            foreach (User friend in i_LoggedInUser.Friends)
            {
                foreach (Post post in friend.Posts)
                {
                    yield return post;
                }
            }
        }

        private static bool isPostFromThisDate(Post i_Post)
        {
            DateTime today = DateTime.Today;

            return i_Post.CreatedTime.HasValue && i_Post.CreatedTime.Value.DayOfYear == today.DayOfYear;
        }

        internal static List<Photo> FetchPhotosFromThePast(User i_LoggedInUser)
        {
            DateTime today = DateTime.Today;
            List<Photo> photosFromThePastList = new List<Photo>();
            foreach (Photo photo in i_LoggedInUser.PhotosTaggedIn)
            {
                if (photo.CreatedTime.HasValue && photo.CreatedTime.Value.DayOfYear == today.DayOfYear)
                {
                    photosFromThePastList.Add(photo);
                }
            }

            return photosFromThePastList;
        }
    }
}

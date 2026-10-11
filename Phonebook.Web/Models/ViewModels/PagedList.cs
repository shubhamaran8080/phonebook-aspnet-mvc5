using System.Collections.Generic;

namespace Phonebook.Web.Models.ViewModels
{
    /// <summary>
    /// Helper for building pagination window (Google-style with ellipsis).
    /// </summary>
    public static class PaginationHelper
    {
        public static List<object> GetPageButtons(int currentPage, int totalPages)
        {
            const int delta = 2;
            const int edgeWindow = 3 + 2 * delta; // 7

            var buttons = new List<object>();

            if (totalPages <= delta * 2 + 2)
            {
                for (int i = 1; i <= totalPages; i++)
                    buttons.Add(i);
                return buttons;
            }

            int left = System.Math.Max(currentPage - delta, 1);
            int right = System.Math.Min(currentPage + delta, totalPages);
            bool showLeftDots = left > 3;
            bool showRightDots = right < totalPages - 2;

            if (!showLeftDots && !showRightDots)
            {
                for (int i = 1; i <= totalPages; i++)
                    buttons.Add(i);
                return buttons;
            }

            if (!showLeftDots)
            {
                for (int i = 1; i <= edgeWindow; i++)
                    buttons.Add(i);
                buttons.Add("...");
                buttons.Add(totalPages);
                return buttons;
            }

            if (!showRightDots)
            {
                buttons.Add(1);
                buttons.Add("...");
                for (int i = totalPages - edgeWindow + 1; i <= totalPages; i++)
                    buttons.Add(i);
                return buttons;
            }

            buttons.Add(1);
            buttons.Add("...");
            for (int i = left; i <= right; i++)
                buttons.Add(i);
            buttons.Add("...");
            buttons.Add(totalPages);
            return buttons;
        }
    }
}

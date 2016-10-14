using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBlockEndTag
{
    static class Extensions
    {

        /// <summary>
        /// Joins another span with the current one
        /// </summary>
        public static Span Join(this Span s1, Span span)
        {
            int start = Math.Min(s1.Start, span.Start);
            int end = Math.Max(s1.End, span.End);
            return new Span(start, end - start);
        }

    }
}

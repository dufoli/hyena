//
// DateQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

using Mono.Unix;

using Hyena;

namespace Hyena.Query
{
    public enum RelativeDateFactor {
        Second = 1,
        Minute = 60,
        Hour   = 3600,
        Day    = 3600*24,
        Week   = 3600*24*7,
        Month  = 3600*24*30,
        Year   = 3600*24*365
    }

    public class DateQueryValue : QueryValue
    {
        public static readonly Operator Equal              = new Operator ("equals", "= {0}", "==", "=", ":");
        public static readonly Operator NotEqual           = new Operator ("notEqual", "!= {0}", true, "!=", "!:");
        public static readonly Operator LessThanEqual      = new Operator ("lessThanEquals", "<= {0}", "<=");
        public static readonly Operator GreaterThanEqual   = new Operator ("greaterThanEquals", ">= {0}", ">=");
        public static readonly Operator LessThan           = new Operator ("lessThan", "< {0}", "<");
        public static readonly Operator GreaterThan        = new Operator ("greaterThan", "> {0}", ">");

        protected DateTime value;
        protected bool relative = false;
        protected long offset = 0;
        protected RelativeDateFactor factor;

        public override string XmlElementName {
            get { return "date"; }
        }

        //protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (Equal, NotEqual, LessThan, GreaterThan, LessThanEqual, GreaterThanEqual);
        protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (LessThanEqual, GreaterThanEqual);
        public override AliasedObjectSet<Operator> OperatorSet {
            get { return operators; }
        }

        public override object Value {
            get { return value; }
        }

        public bool Relative {
            get { return relative; }
        }

        public RelativeDateFactor Factor {
            get { return factor; }
        }

        public long RelativeOffset {
            get { return offset; }
        }

        private static Regex number_regex = new Regex ("\\d+", RegexOptions.Compiled);
        public override void ParseUserQuery (string input)
        {
            try {
                value = DateTime.Parse (input);
                IsEmpty = false;
            } catch {
                Match match = number_regex.Match (input);
                if (match != Match.Empty && match.Groups.Count > 0) {
                    int val = Convert.ToInt32 (match.Groups[0].Captures[0].Value);
                    foreach (RelativeDateFactor factor in Enum.GetValues (typeof(RelativeDateFactor))) {
                        if (input == FactorString (factor, val)) {
                            SetRelativeValue ((long) -val, factor);
                            return;
                        }
                    }
                }
                IsEmpty = true;
            }
        }

        public override string ToUserQuery ()
        {
            if (relative) {
                return FactorString (factor, (int) (RelativeOffset == 0 ? 0 : (-RelativeOffset / (long) factor)));
            } else {
                if (value.Hour == 0 && value.Minute == 0 && value.Second == 0) {
                    return value.ToString ("yyyy-MM-dd");
                } else {
                    return value.ToString ();
                }
            }
        }

        public void SetValue (DateTime date)
        {
            value = date;
            relative = false;
            IsEmpty = false;
        }

        public void SetRelativeValue (long offset, RelativeDateFactor factor)
        {
            this.factor = factor;
            this.offset = offset * (long) factor;
            relative = true;
            IsEmpty = false;
        }

        public void LoadString (string val, bool isRelative)
        {
            try {
                if (isRelative) {
                    SetRelativeValue (Convert.ToInt64 (val), RelativeDateFactor.Second);
                    DetermineFactor ();
                } else {
                    SetValue (DateTime.Parse (val));
                }
            } catch {
                IsEmpty = true;
            }
        }

        protected void DetermineFactor ()
        {
            if (relative) {
                long val = Math.Abs (offset);
                foreach (RelativeDateFactor factor in Enum.GetValues (typeof(RelativeDateFactor))) {
                    if (val >= (long) factor) {
                        this.factor = factor;
                    }
                }
            }
        }

        public override void ParseXml (XmlElement node)
        {
            try {
                LoadString (node.InnerText, node.HasAttribute ("type") && node.GetAttribute ("type") == "rel");
            } catch {
                IsEmpty = true;
            }
        }

        public override void AppendXml (XmlElement node)
        {
            if (relative) {
                node.SetAttribute ("type", "rel");
                node.InnerText = RelativeOffset.ToString ();
            } else {
                base.AppendXml (node);
            }
        }

        public override string ToSql ()
        {
            return DateTimeUtil.FromDateTime (
                (relative ? DateTime.Now + TimeSpan.FromSeconds ((double) offset) : value)
            ).ToString ();
        }

        public DateTime DateTime {
            get { return value; }
        }

        protected static string FactorString (RelativeDateFactor factor, int count)
        {
            string translated = null;
            switch (factor) {
                case RelativeDateFactor.Second: translated = Catalog.GetPluralString ("{0} second", "{0} seconds", count); break;
                case RelativeDateFactor.Minute: translated = Catalog.GetPluralString ("{0} minute", "{0} minutes", count); break;
                case RelativeDateFactor.Hour:   translated = Catalog.GetPluralString ("{0} hour",   "{0} hours", count); break;
                case RelativeDateFactor.Day:    translated = Catalog.GetPluralString ("{0} day",    "{0} days", count); break;
                case RelativeDateFactor.Week:   translated = Catalog.GetPluralString ("{0} week",   "{0} weeks", count); break;
                case RelativeDateFactor.Month:  translated = Catalog.GetPluralString ("{0} month",  "{0} months", count); break;
                case RelativeDateFactor.Year:   translated = Catalog.GetPluralString ("{0} year",   "{0} years", count); break;
                default: return null;
            }

            return String.Format (
                Catalog.GetString ("{0} ago"),
                String.Format (translated, count)
            );
        }
    }
}

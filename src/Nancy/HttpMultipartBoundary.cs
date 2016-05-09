namespace Nancy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Nancy.Helpers;

    /// <summary>
    /// Represents the content boundary of a HTTP multipart/form-data boundary in a stream.
    /// </summary>
    public class HttpMultipartBoundary
    {
        private const byte LF = (byte)'\n';
        private const byte CR = (byte)'\r';

        /// <summary>
        /// A regular expression used to extract the name parameter from a Content-Disposition header.
        /// </summary>
        /// <value>A <see cref="Regex"/> object.</value>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Regex NameExpression = new Regex(@"name=""?(?<name>[^\""]*)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// A regular expression used to extract the filename parameter(s) from a Content-Disposition header.
        /// </summary>
        /// <value>A <see cref="Regex"/> object.</value>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Regex FilenameExpression = new Regex(
                @"(?:filename=""?(?<filename>[^\"";]*))?(?:filename\*=(?<enc>utf-8|iso-8859-1)'(?<lang>[\w\-]*)?'(?<filenamestar>[^\"";]*))?",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMultipartBoundary"/> class.
        /// </summary>
        /// <param name="boundaryStream">The stream that contains the boundary information.</param>
        public HttpMultipartBoundary(HttpMultipartSubStream boundaryStream)
        {
            this.Value = boundaryStream;
            this.ExtractHeaders();
        }

        /// <summary>
        /// Gets the contents type of the boundary value.
        /// </summary>
        /// <value>A <see cref="string"/> containing the name of the value if it is available; otherwise <see cref="string.Empty"/>.</value>
        public string ContentType { get; private set; }

        /// <summary>
        /// Gets or the filename for the boundary value.
        /// </summary>
        /// <value>A <see cref="string"/> containing the filename value if it is available; otherwise <see cref="string.Empty"/>.</value>
        /// <remarks>This is the RFC5987 decoded value of the filename attribute of the Content-Disposition header.</remarks>
        public string Filename { get; private set; }

        /// <summary>
        /// Gets name of the boundary value.
        /// </summary>
        /// <remarks>This is the RFC2047 decoded value of the name attribute of the Content-Disposition header.</remarks>
        public string Name { get; private set; }

        /// <summary>
        /// A stream containing the value of the boundary.
        /// </summary>
        /// <remarks>This is the RFC2047 decoded value of the Content-Type header.</remarks>
        public HttpMultipartSubStream Value { get; private set; }

        private void ExtractHeaders()
        {
            while(true)
            {
                var header = ReadLineFromStream(this.Value);

                if (string.IsNullOrEmpty(header))
                {
                    break;
                }

                if (header.StartsWith("Content-Disposition", StringComparison.CurrentCultureIgnoreCase))
                {
                    this.Name = NameExpression.Match(header).Groups["name"].Value;
                    var filenameMatch = FilenameExpression.Match(header);
                    var filenameStar = filenameMatch.Groups["filenamestar"].Value;
                    this.Filename = !string.IsNullOrEmpty(filenameStar) 
                        ? HttpUtility.UrlDecode(filenameStar) 
                        : filenameMatch.Groups["filename"].Value;
                }

                if (header.StartsWith("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    this.ContentType = header.Split(new[] { ' ' }).Last().Trim();
                }
            }

            this.Value.PositionStartAtCurrentLocation();
        }

        private static string ReadLineFromStream(Stream stream)
        {
            var readBuffer = new List<byte>();

            while (true)
            {
                var byteReadFromStream = stream.ReadByte();

                if (byteReadFromStream == -1)
                {
                    return null;
                }

                if (byteReadFromStream.Equals(LF))
                {
                    break;
                }

                readBuffer.Add((byte) byteReadFromStream);
            }

            return Encoding.UTF8.GetString(readBuffer.ToArray()).Trim((char) CR);
        }
    }
}
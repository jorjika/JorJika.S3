using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class ObjectNameIsNotValidException : S3BaseException
    {
        public ObjectNameIsNotValidException() : 
                base("Object name is not valid.", 
                    $"Object name is not valid. Allowed characters are 'a-z', 'A-Z', '/' and '.'. Its not allowd to use '/' this character at the end of the file name.")
        {

        }
    }
}

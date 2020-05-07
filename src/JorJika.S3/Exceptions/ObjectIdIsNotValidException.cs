using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class ObjectIdIsNotValidException : S3BaseException
    {
        public ObjectIdIsNotValidException() : 
                base("Object Id is not valid.", 
                    $"Object Id is not valid. It should be like 'bucketname/objectname'. Allowed characters are 'a-z', 'A-Z', '/' and '.'. Its not allowd to use '/' this character at the end of the file name.")
        {

        }
    }
}

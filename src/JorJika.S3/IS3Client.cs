using JorJika.S3.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JorJika.S3.Exceptions;

namespace JorJika.S3
{
    /// <summary>
    /// S3 client interface
    /// </summary>
    public interface IS3Client
    {
        #region Bucket Operations

        /// <summary>
        /// Create bucket
        /// </summary>
        /// <param name="bucketName">Bucket name - Validation: lower case alpha numeric characters plus dots.</param>
        /// <returns></returns>
        /// <exception cref="BucketNameIsNotValidException">Thrown when bucket name is invalid.</exception>
        /// <exception cref="BucketExistsException">Thrown when bucket already exists with this name.</exception>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task CreateBucket(string bucketName);

        /// <summary>
        /// Remove bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        /// <exception cref="BucketNotFoundException">Thrown when bucket does not exist.</exception>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task RemoveBucket(string bucketName);

        #endregion

        #region Object Operations

        /// <summary>
        /// Gets object information - Without file data
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Returns object information and metadata</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="ObjectNotFoundException">Thrown when object is not found.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task<S3Object> GetObjectInfo(string objectId);

        /// <summary>
        /// Get object - With data
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Returns actual object data - bytes</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="ObjectNotFoundException">Thrown when object is not found.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task<S3Object> GetObject(string objectId);

        /// <summary>
        /// Gets object URL for downloading from storage (Temporary URL support varies by implementation)
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <param name="expiresInSeconds">Temporary link expiration time in seconds. Defaults to 12 hours</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Returns temporary URL of object for download</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="ObjectNotFoundException">Thrown when object is not found.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task<string> GetObjectURL(string objectId, int expiresInSeconds = 10 * 60);

        /// <summary>
        /// Save binary object
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <param name="objectData">Object byte array</param>
        /// <param name="contentType">Content type - Optional (Used for PDF and text files to directly show in browser when issuing temporary link)</param>
        /// <param name="metaData">Object meta data</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Object Id</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task<string> SaveObject(string objectName, byte[] objectData, string contentType = null, Dictionary<string, string> metaData = null, string bucketName = null);

        /// <summary>
        /// Removes object from storage
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns></returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task RemoveObject(string objectId);

        /// <summary>
        /// Save text file to storage
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <param name="content">Text file content</param>
        /// <param name="fileName">File name - Optional (If you are downloading file from browser file name is automatically filled with this value)</param>
        /// <param name="fileExtension">File extension, defaults to txt.</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Object Id</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task<string> SaveText(string objectName, string content, string fileName = null, string fileExtension = "txt", string bucketName = null);

        /// <summary>
        /// Save pdf file to storage
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <param name="objectData">PDF file byte array</param>
        /// <param name="fileName">File name - Optional (If you are downloading file from browser file name is automatically filled with this value)</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Object Id</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        Task<string> SavePDF(string objectName, byte[] objectData, string fileName = null, string bucketName = null);

        #endregion
    }
}

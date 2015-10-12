namespace ElmahR.Persistence.EntityFramework
{
    #region Imports

    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    #endregion

    class ErrorLogEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(255)]
        public string Key { get; set; }

        [StringLength(255)]
        public string ApplicationId { get; set; }

        [Column(TypeName = "ntext")]
        [MaxLength]
        public string Body { get; set; }

        [StringLength(255)]
        public string Type { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ReceivedAt { get; set; }
    }

    class UserSetting
    {
        [Key]
        [StringLength(255)]
        public string UserId { get; set; }

        [StringLength(255)]
        public string Key { get; set; }

        [StringLength(255)]
        public string Specifier { get; set; }

        [Column(TypeName = "ntext")]
        public string Value { get; set; }
    }
}

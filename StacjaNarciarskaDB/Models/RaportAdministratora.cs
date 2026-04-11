using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class RaportAdministratora
{
    public int Id { get; set; }

    public int? AdministratorId { get; set; }

    public int? RodzajRaportuId { get; set; }

    public DateTime? DataWygenerowania { get; set; }

    public string? ParametryRaportu { get; set; }

    public virtual Administrator? Administrator { get; set; }

    public virtual RodzajRaportu? RodzajRaportu { get; set; }
}

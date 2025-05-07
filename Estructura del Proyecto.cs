public class AgenciaIndexViewModel
{
    public AgenciaModel AgenciaEnEdicion { get; set; } = new();
    public IPagedList<AgenciaModel> Lista { get; set; } = new List<AgenciaModel>().ToPagedList(1, 1);
    public List<SelectListItem> AgenciasFiltro { get; set; } = new();
    public int? CodccoSeleccionado { get; set; }
}

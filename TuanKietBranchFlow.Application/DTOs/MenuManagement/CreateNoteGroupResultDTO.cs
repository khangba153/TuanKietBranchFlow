namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateNoteGroupResultDTO
{
    public bool IsNameInvalid { get; set; }
    public bool IsNameDuplicated { get; set; }
    public MenuNoteGroupDTO? NoteGroup { get; set; }
}

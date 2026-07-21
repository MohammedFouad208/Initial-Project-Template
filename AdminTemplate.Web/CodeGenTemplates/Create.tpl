@model AdminTemplate.Domain.Entities.{{ entity.name }}
@{
    var isEdit     = ViewBag.IsEdit == true;
    var itemId     = ViewBag.ItemId as Guid?;
    var pageTitle  = isEdit ? Localizer["{{ entity.name }}_Edit_Title"].Value : Localizer["{{ entity.name }}_Create_Title"].Value;
    var formAction = isEdit ? "Edit" : "Create";
    ViewData["Title"] = pageTitle;
}

<div class="page-header d-flex align-items-start justify-content-between flex-wrap gap-3">
    <div>
        <h1 class="mb-1">@pageTitle</h1>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
                <li class="breadcrumb-item">
                    <a asp-controller="Dashboard" asp-action="Index" class="text-decoration-none" style="color:var(--primary)">@Localizer["Common_Home"]</a>
                </li>
                <li class="breadcrumb-item">
                    <a asp-action="Index" class="text-decoration-none" style="color:var(--primary)">@Localizer["{{ entity.name }}_Index_Title"]</a>
                </li>
                <li class="breadcrumb-item active" aria-current="page">@(isEdit ? Localizer["Common_Edit"].Value : Localizer["Common_Create"].Value)</li>
            </ol>
        </nav>
    </div>
</div>

<div class="card">
    <div class="card-body">
        @if (!ViewData.ModelState.IsValid)
        {
            <div asp-validation-summary="All"
                 class="alert alert-danger mb-4"
                 style="border-inline-start:4px solid var(--danger)"
                 role="alert">
            </div>
        }

        <form asp-action="@formAction" asp-route-id="@itemId" method="post" novalidate>
            @Html.AntiForgeryToken()
            <div class="row g-3">
                {{- for col in entity.columns }}
                {{- if col.show_in_form }}
                <div class="col-md-6">
                    <label asp-for="{{ col.name }}" class="form-label">@Localizer["{{ entity.name }}_Column_{{ col.name }}"]</label>
                    {{- if col.data_type == "bool" }}
                    <div class="form-check">
                        <input asp-for="{{ col.name }}" class="form-check-input" type="checkbox" />
                        <label asp-for="{{ col.name }}" class="form-check-label">@Localizer["{{ entity.name }}_Column_{{ col.name }}"]</label>
                    </div>
                    {{- else if col.data_type == "datetime" }}
                    <input asp-for="{{ col.name }}" class="form-control" type="datetime-local"
                           {{- if col.is_required }}
                           value="@(Model.{{ col.name }} == default ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm") : Model.{{ col.name }}.ToString("yyyy-MM-ddTHH:mm"))" />
                           {{- else }}
                           value="@(Model.{{ col.name }} == null ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm") : Model.{{ col.name }}.Value.ToString("yyyy-MM-ddTHH:mm"))" />
                           {{- end }}
                    {{- else if col.data_type == "int" || col.data_type == "long" || col.data_type == "decimal" || col.data_type == "double" }}
                    <input asp-for="{{ col.name }}" class="form-control" type="number" />
                    {{- else }}
                    <input asp-for="{{ col.name }}" class="form-control" />
                    {{- end }}
                    <span asp-validation-for="{{ col.name }}" class="text-danger small"></span>
                </div>
                {{- end }}
                {{- end }}
                {{- for rel in entity.relations }}
                <div class="col-md-6">
                    <label asp-for="{{ rel.foreign_key_name }}" class="form-label">@Localizer["{{ entity.name }}_Rel_{{ rel.related_entity_name }}"]</label>
                    <select asp-for="{{ rel.foreign_key_name }}"
                            asp-items="ViewBag.{{ rel.related_entity_name }}List"
                            class="form-select">
                        <option value="">@Localizer["Common_Select"]</option>
                    </select>
                    <span asp-validation-for="{{ rel.foreign_key_name }}" class="text-danger small"></span>
                </div>
                {{- end }}
            </div>

            <div class="d-flex gap-2 mt-4">
                <button type="submit" id="submitBtn" class="btn btn-primary btn-md">
                    <i class="fa-solid fa-floppy-disk me-1"></i>@(isEdit ? Localizer["Common_SaveChanges"].Value : Localizer["Common_Create"].Value)
                </button>
                <a asp-action="Index" class="btn btn-outline-secondary btn-md">@Localizer["Common_Cancel"]</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script>
        (function () {
            var $form = $('form');
            var $btn  = $('#submitBtn');
            var isEdit = @Json.Serialize(isEdit);
            var savingHtml = '<i class="fa-solid fa-spinner fa-spin me-1"></i>@Localizer["Common_Saving"]';
            var saveChangesHtml = '<i class="fa-solid fa-floppy-disk me-1"></i>@Localizer["Common_SaveChanges"]';
            var createHtml = '<i class="fa-solid fa-floppy-disk me-1"></i>@Localizer["Common_Create"]';

            $form.on('submit', function () {
                if ($form.valid()) {
                    $btn.prop('disabled', true).html(savingHtml);
                }
            });

            $form.on('input change', ':input', function () {
                if ($btn.prop('disabled')) {
                    $btn.prop('disabled', false).html(isEdit ? saveChangesHtml : createHtml);
                }
            });
        }());
    </script>
}

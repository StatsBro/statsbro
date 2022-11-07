using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StatsBro.Panel.Models.Forms
{
    public class FormModel
    {
        public List<string> Errors { get; } = new List<string>();

        public void LoadErrors(ModelStateDictionary modelState)
        {
            foreach(var state in modelState.Values)
            {
                foreach(var er in state.Errors)
                {
                    Errors.Add(er.ErrorMessage);
                }
            }
        }

    }
}

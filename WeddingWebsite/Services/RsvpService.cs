using Microsoft.AspNetCore.Authorization;
using WeddingWebsite.Core;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Data.Stores;
using WeddingWebsite.Models.Rsvp;

namespace WeddingWebsite.Services;

[Authorize]
public class RsvpService(IRsvpStore rsvpStore, ICurrentUserContext currentUserContext) : IRsvpService
{
    public bool SubmitRsvp(string guestId, bool isAttending, IReadOnlyList<string?> data)
    {
        currentUserContext.EnsureAuthenticated();
        return rsvpStore.SubmitRsvp(guestId, isAttending, data);
    }
    
    public IEnumerable<RsvpResponse> GetAllRsvps(bool isAttending, RsvpQuestions questions)
    {
        currentUserContext.EnsureInRole("Admin");
        var rsvps = rsvpStore.GetAllRsvps();
        return rsvps.Where(rsvp => rsvp.IsAttending == isAttending).Select(rsvp =>
        {
            var dataByColumn = new Dictionary<string, string?>();
            foreach (var column in questions.GetAllColumns().Where(col => col.DisplayName != null))
            {
                var value = rsvp.Data.ElementAtOrDefault(column.Id);
                dataByColumn[column.DisplayName!] = value;
            }
            var dataByQuestion = new Dictionary<string, string>();
            foreach (var question in questions.Questions)
            {
                var value = question.QuestionType.GetAnswerString(rsvp.Data) ?? "<null>";
                dataByQuestion[question.Title] = value;
            }
            return new RsvpResponse(rsvp.GuestId, rsvp.GuestName, rsvp.IsAttending, dataByColumn, dataByQuestion);
        });
    }
    
    public RsvpResponse? GetRsvp(string guestId, RsvpQuestions yesQuestions, RsvpQuestions noQuestions)
    {
        currentUserContext.EnsureInRole("Admin");
        var rsvp = rsvpStore.GetRsvp(guestId);
        if (rsvp == null) return null;

        var questions = rsvp.IsAttending ? yesQuestions : noQuestions;
        
        var dataByColumn = new Dictionary<string, string?>();
        foreach (var column in questions.GetAllColumns().Where(col => col.DisplayName != null))
        {
            var value = rsvp.Data.ElementAtOrDefault(column.Id);
            dataByColumn[column.DisplayName!] = value;
        }
        var dataByQuestion = new Dictionary<string, string>();
        foreach (var question in questions.Questions)
        {
            var value = question.QuestionType.GetAnswerString(rsvp.Data);
            value ??= "[No Data - This question has been added after the RSVP form was submitted]";
            if (value == string.Empty)
            {
                value = "[Blank]";
            }
            dataByQuestion[question.Title] = value;
        }
        return new RsvpResponse(rsvp.GuestId, rsvp.GuestName, rsvp.IsAttending, dataByColumn, dataByQuestion);
    }
    
    public RsvpResponseData? GetRsvpBasic(string guestId)
    {
        currentUserContext.EnsureAuthenticated();
        return rsvpStore.GetRsvp(guestId);
    }
    
    public void DeleteRsvp(string guestId)
    {
        currentUserContext.EnsureInRole("Admin");
        rsvpStore.DeleteRsvp(guestId);
    }

    public bool EditRsvp(string guestId, bool isAttending, IReadOnlyList<string?> data)
    {
        currentUserContext.EnsureAuthenticated();
        rsvpStore.DeleteRsvp(guestId);
        return rsvpStore.SubmitRsvp(guestId, isAttending, data);
    }
}

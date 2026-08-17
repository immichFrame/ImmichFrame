using System.ComponentModel.DataAnnotations;
using ImmichFrame.Core.Events;

namespace ImmichFrame.WebApi.Models.Events;

public class FrameEventAckRequestDto
{
    [Required]
    public FrameEventAckStatus? Status { get; set; }
}

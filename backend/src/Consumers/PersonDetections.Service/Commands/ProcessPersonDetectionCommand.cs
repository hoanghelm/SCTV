using Common.ApiResponse;
using MediatR;
using PersonDetections.Service.Models;

namespace PersonDetections.Service.Commands;

public class ProcessPersonDetectionCommand : IRequest<bool>
{
    public PersonDetectionMessage DetectionMessage { get; set; }
    
    public ProcessPersonDetectionCommand(PersonDetectionMessage detectionMessage)
    {
        DetectionMessage = detectionMessage;
    }
}
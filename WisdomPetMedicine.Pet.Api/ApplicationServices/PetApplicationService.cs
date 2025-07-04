﻿using Dapr.Client;
using WisdomPetMedicine.Pet.Api.Commands;
using WisdomPetMedicine.Pet.Api.IntegrationEvents;
using WisdomPetMedicine.Pet.Domain.Events;
using WisdomPetMedicine.Pet.Domain.Repositories;
using WisdomPetMedicine.Pet.Domain.Services;
using WisdomPetMedicine.Pet.Domain.ValueObjects;

namespace WisdomPetMedicine.Pet.Api.ApplicationServices;

public class PetApplicationService
{
    private const string PubSubName = "pubsub";
    private readonly IPetRepository petRepository;
    private readonly IBreedService breedService;
    private readonly ILogger<PetApplicationService> logger;
    private readonly DaprClient daprClient;

    public PetApplicationService(IPetRepository petRepository,
                                 IBreedService breedService,
                                 ILogger<PetApplicationService> logger,
                                 DaprClient daprClient)
    {
        this.petRepository = petRepository;
        this.breedService = breedService;
        this.logger = logger;
        this.daprClient = daprClient;

        DomainEvents.PetFlaggedForAdoption.Register(async c =>
        {
            var integrationEvent = new PetFlaggedForAdoptionIntegrationEvent(c.Id,
                                                                             c.Name,
                                                                             c.Breed,
                                                                             c.Sex,
                                                                             c.Color,
                                                                             c.DateOfBirth,
                                                                             c.Species);
            await daprClient.PublishEventAsync(PubSubName, "pet-flagged-for-adoption", integrationEvent);
        });

        DomainEvents.PetTransferredToHospital.Register(async c =>
        {
            var integrationEvent = new PetTransferredToHospitalIntegrationEvent(c.Id,
                                                                             c.Name,
                                                                             c.Breed,
                                                                             c.Sex,
                                                                             c.Color,
                                                                             c.DateOfBirth,
                                                                             c.Species);
            await daprClient.PublishEventAsync(PubSubName, "pet-transferred-to-hospital", integrationEvent);
        });
    }

    public async Task HandleCommandAsync(CreatePetCommand command)
    {
        var pet = new Domain.Entities.Pet(PetId.Create(command.Id));
        pet.SetName(PetName.Create(command.Name));
        pet.SetBreed(PetBreed.Create(command.Breed, breedService));
        pet.SetSex(SexOfPet.Create((SexesOfPets)command.Sex));
        pet.SetColor(PetColor.Create(command.Color));
        pet.SetDateOfBirth(PetDateOfBirth.Create(command.DateOfBirth));
        pet.SetSpecies(PetSpecies.Get(command.Species));
        await petRepository.AddAsync(pet);
    }

    public async Task HandleCommandAsync(SetNameCommand command)
    {
        var pet = await petRepository.GetAsync(PetId.Create(command.Id));
        pet.SetName(PetName.Create(command.Name));
        await petRepository.UpdateAsync(pet);
    }

    public async Task HandleCommandAsync(SetBreedCommand command)
    {
        var pet = await petRepository.GetAsync(PetId.Create(command.Id));
        pet.SetBreed(PetBreed.Create(command.Breed, new FakeBreedService()));
        await petRepository.UpdateAsync(pet);
    }

    public async Task HandleCommandAsync(SetColorCommand command)
    {
        var pet = await petRepository.GetAsync(PetId.Create(command.Id));
        pet.SetColor(PetColor.Create(command.Color));
        await petRepository.UpdateAsync(pet);
    }

    public async Task HandleCommandAsync(SetDateOfBirthCommand command)
    {
        var pet = await petRepository.GetAsync(PetId.Create(command.Id));
        pet.SetDateOfBirth(PetDateOfBirth.Create(command.DateOfBirth));
        await petRepository.UpdateAsync(pet);
    }

    public async Task HandleCommandAsync(FlagForAdoptionCommand command)
    {
        var pet = await petRepository.GetAsync(PetId.Create(command.Id));
        pet.FlagForAdoption();
    }

    public async Task HandleCommandAsync(TransferToHospitalCommand command)
    {
        var pet = await petRepository.GetAsync(PetId.Create(command.Id));
        pet.TransferToHospital();
    }
}
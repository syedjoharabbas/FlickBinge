using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Core.Entities;
using UserService.Core.Interfaces;
using UserService.Infrastructure.RabbitMQ;

namespace UserService.Infrastructure.Services
{
    public class UserService
    {
        private readonly IUserRepository _repository;
        private readonly RabbitMQPublisher _publisher;

        public UserService(IUserRepository repository, RabbitMQPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        // Get all users
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _repository.GetAllAsync();
        }

        // Get a single user by ID
        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // Create a new user
        public async Task<User> CreateUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Default created date
            user.CreatedAt = DateTime.UtcNow;

            // Save user in DB
            var createdUser = await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            // Trigger UserCreated event
            await _publisher.PublishUserCreatedAsync(createdUser.Id);

            return createdUser;
        }

        // Update an existing user
        public async Task<User?> UpdateUserAsync(Guid id, User user)
        {
            var existingUser = await _repository.GetByIdAsync(id);
            if (existingUser == null) return null;

            // Update properties (business rules can go here)
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;

            var updatedUser = await _repository.UpdateAsync(existingUser);
            await _repository.SaveChangesAsync();
            return updatedUser;
        }

        // Delete a user
        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var success = await _repository.DeleteAsync(id);
            if (success)
                await _repository.SaveChangesAsync();

            return success;
        }
    }
}

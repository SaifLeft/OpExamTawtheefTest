/**
 * App Chat
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  // Initialize chat functionality
  const chatHistory = document.querySelector('.chat-history');
  const messageInput = document.querySelector('.message-input');
  const sendButton = document.querySelector('.send-msg-btn');
  const fileInput = document.getElementById('Attachment');
  const fileLabel = document.querySelector('label[for="Attachment"]');

  // Scroll to bottom of chat history
  function scrollToBottom() {
    if (chatHistory) {
      chatHistory.scrollTop = chatHistory.scrollHeight;
    }
  }

  // Handle form submission
  const form = document.querySelector('.form-send-message');
  if (form) {
    form.addEventListener('submit', function (e) {
      if (!messageInput.value.trim() && !fileInput.files.length) {
        e.preventDefault();
        return;
      }
    });
  }

  // Handle file selection
  if (fileInput) {
    fileInput.addEventListener('change', function() {
      if (fileInput.files.length > 0) {
        const fileName = fileInput.files[0].name;
        fileLabel.innerHTML = `<i class="bx bx-paperclip"></i> <span class="d-none d-md-inline-block">${fileName}</span>`;
        fileLabel.classList.add('btn-primary');
        fileLabel.classList.remove('btn-outline-primary');
      } else {
        fileLabel.innerHTML = '<i class="bx bx-paperclip"></i>';
        fileLabel.classList.remove('btn-primary');
        fileLabel.classList.add('btn-outline-primary');
      }
    });
  }

  // Handle sidebar toggle
  const sidebarToggles = document.querySelectorAll('[data-bs-toggle="sidebar"]');
  sidebarToggles.forEach(toggle => {
    toggle.addEventListener('click', function() {
      const target = document.querySelector(this.getAttribute('data-target'));
      if (target) {
        target.classList.toggle('show');
      }
    });
  });

  // Scroll to bottom on page load
  scrollToBottom();
});

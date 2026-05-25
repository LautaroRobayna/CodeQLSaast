import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

Given('existe una reserva pendiente creada hace {int} días con clave pública {string}',
  (daysAgo: number, publicKey: string) => {
    const reservationDate = new Date();
    reservationDate.setDate(reservationDate.getDate() - daysAgo);

    cy.intercept('GET', '**/api/reservation*', {
      statusCode: 200,
      body: {
        id: 1,
        code: 'RES-EXP-001',
        publicKey: publicKey,
        userEmail: 'cliente@example.com',
        reservationDate: reservationDate.toISOString(),
        expirationDate: new Date(reservationDate.getTime() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
        status: 'Expired',
        prescriptionUploaded: false,
        details: [
          { id: 1, drugCode: 'PAR-500', drugName: 'Paracetamol 500mg', quantity: 3, requiresPrescription: false }
        ]
      }
    }).as('getReservation');
  }
);

Given('el cliente visita la página {string}', (url: string) => {
  cy.visit(`http://localhost:4200${url}`);
});

Given('ingresa la clave pública {string} en el campo {string}', (publicKey: string, selector: string) => {
  cy.get(selector).clear().type(publicKey);
});

Given('busca la reserva', () => {
  cy.get('#btn-buscar-reserva').click();
  cy.wait('@getReservation');
});

Then('el sistema debe mostrar la reserva como {string}', (status: string) => {
  cy.get('[data-cy=reservation-status]').should('contain', status);
});

Then('el sistema debe mostrar el mensaje {string}', (message: string) => {
  cy.get('[data-cy=reservation-message]').should('contain', message);
});

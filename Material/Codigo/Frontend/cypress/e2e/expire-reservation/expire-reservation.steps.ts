import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

let reservations: any[] = [];

beforeEach(() => {
  reservations = [];
});

Given('existe una reserva pendiente creada hace {int} días con clave pública {string}',
  (daysAgo: number, publicKey: string) => {
    const reservationDate = new Date();
    reservationDate.setDate(reservationDate.getDate() - daysAgo);

    const isExpired = daysAgo > 30;

    reservations.push({
      id: reservations.length + 1,
      code: `RES-EXP-00${reservations.length + 1}`,
      publicKey: publicKey,
      userEmail: 'cliente@example.com',
      reservationDate: reservationDate.toISOString(),
      expirationDate: new Date(reservationDate.getTime() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      status: isExpired ? 'Expired' : 'Pending',
      prescriptionUploaded: false,
      details: [
        { id: 1, drugCode: 'PAR-500', drugName: 'Paracetamol 500mg', quantity: 3, requiresPrescription: false }
      ]
    });
  }
);

Given('el cliente visita la página {string}', (url: string) => {
  cy.visit(`http://localhost:4200${url}`);
});

Given('ingresa la clave pública {string} en el campo {string}', (publicKey: string, selector: string) => {
  cy.get(selector).clear().type(publicKey);
});

Given('busca la reserva', () => {
  cy.intercept('GET', '**/api/reservation*', {
    statusCode: 200,
    body: reservations[0]
  }).as('getReservation');
  cy.get('#btn-buscar-reserva').click();
  cy.wait('@getReservation');
});

When('el empleado solicita todas las reservas pendientes', () => {
  const pendingReservations = reservations.filter(r => r.status === 'Pending');

  cy.intercept('GET', '**/api/reservation*', {
    statusCode: 200,
    body: pendingReservations
  }).as('getAllPending');
  cy.visit('http://localhost:4200/reservations/manage');
  cy.wait('@getAllPending');
});

Then('solo debe aparecer la reserva {string}', (publicKey: string) => {
  cy.get('[data-cy=reservation-row]').should('have.length', 1);
  cy.get('[data-cy=reservation-row]').should('contain', publicKey);
});

Then('el sistema debe mostrar la reserva como {string}', (status: string) => {
  cy.get('[data-cy=reservation-status]').should('contain', status);
});

Then('el sistema debe mostrar el mensaje {string}', (message: string) => {
  cy.get('[data-cy=reservation-message]').should('contain', message);
});

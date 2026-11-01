#include <iostream>
#include <vector>
#include <cstdlib>
#include <ctime>
#include <chrono>
#include <thread>

#ifdef _WIN32
#include <conio.h>
#else
#include <termios.h>
#include <unistd.h>
#include <fcntl.h>
#endif

struct FallingItem {
    int x;
    int y;
    bool isBomb;
};

#ifndef _WIN32
// Configure terminal for non-blocking key reads on macOS / Linux
class TerminalMode {
    struct termios origTermios;
public:
    TerminalMode() {
        tcgetattr(STDIN_FILENO, &origTermios);
        struct termios raw = origTermios;
        raw.c_lflag &= ~(ICANON | ECHO);
        tcsetattr(STDIN_FILENO, TCSANOW, &raw);
        int flags = fcntl(STDIN_FILENO, F_GETFL, 0);
        fcntl(STDIN_FILENO, F_SETFL, flags | O_NONBLOCK);
    }
    ~TerminalMode() {
        tcsetattr(STDIN_FILENO, TCSANOW, &origTermios);
    }
};

int ReadKeyNonBlocking() {
    unsigned char ch;
    if (read(STDIN_FILENO, &ch, 1) == 1) {
        if (ch == 27) { // Arrow key escape sequence
            unsigned char seq[2];
            if (read(STDIN_FILENO, &seq[0], 1) == 1 &&
                read(STDIN_FILENO, &seq[1], 1) == 1 &&
                seq[0] == '[') {
                if (seq[1] == 'D') return 'a'; // Left arrow
                if (seq[1] == 'C') return 'd'; // Right arrow
            }
            return 0;
        }
        return ch;
    }
    return 0;
}
#else
int ReadKeyNonBlocking() {
    if (_kbhit()) {
        int ch = _getch();
        if (ch == 0 || ch == 224) {
            int arrow = _getch();
            if (arrow == 75) return 'a'; // Left arrow
            if (arrow == 77) return 'd'; // Right arrow
            return 0;
        }
        return ch;
    }
    return 0;
}
#endif

int main() {
    std::srand(static_cast<unsigned int>(std::time(nullptr)));

#ifndef _WIN32
    TerminalMode termGuard;
#endif

    const int width = 30;
    const int height = 16;
    const int maxBombs = 3;
    const int basketRadius = 2; // Basket spans [playerX - 2, playerX + 2]

    bool quitApp = false;

    while (!quitApp) {
        int playerX = width / 2;
        int score = 0;
        int bombs = 0;
        int tick = 0;
        bool isGameOver = false;
        std::vector<FallingItem> items;

        // Clear screen once at game start
        std::cout << "\033[2J";

        while (!isGameOver && !quitApp) {
            // 1. Read all queued key presses for responsive movement
            int key = 0;
            while ((key = ReadKeyNonBlocking()) != 0) {
                if (key == 'a' || key == 'A') {
                    if (playerX - basketRadius > 1) playerX -= 2;
                } else if (key == 'd' || key == 'D') {
                    if (playerX + basketRadius < width - 2) playerX += 2;
                } else if (key == 'q' || key == 'Q') {
                    quitApp = true;
                }
            }

            // 2. Spawn Coins ($) and Bombs (*) periodically
            tick++;
            if (tick % 3 == 0) {
                FallingItem item;
                item.x = 1 + (std::rand() % (width - 2));
                item.y = 1;
                item.isBomb = (std::rand() % 100) < 30; // 30% bomb chance
                items.push_back(item);
            }

            // 3. Move items down & check collisions with Player Basket
            for (int i = static_cast<int>(items.size()) - 1; i >= 0; --i) {
                items[i].y += 1;

                // Check if item reached basket row
                if (items[i].y == height - 2 &&
                    std::abs(items[i].x - playerX) <= basketRadius) {
                    if (items[i].isBomb) {
                        bombs++;
                        if (bombs >= maxBombs) {
                            isGameOver = true;
                        }
                    } else {
                        score++;
                    }
                    items.erase(items.begin() + i);
                    continue;
                }

                // Remove item if it falls past the bottom
                if (items[i].y >= height - 1) {
                    items.erase(items.begin() + i);
                }
            }

            // 4. Render frame in terminal
            std::cout << "\033[H"; // Move cursor to top-left
            std::cout << "=== COIN CATCHER (C++) ===\n";
            std::cout << "Score: " << score
                      << "   Bombs: " << bombs << " / " << maxBombs << "   \n";
            std::cout << "Controls: [A/D] or [Left/Right] Move | [Q] Quit\n";

            for (int y = 0; y < height; ++y) {
                for (int x = 0; x < width; ++x) {
                    if (y == 0 || y == height - 1 || x == 0 || x == width - 1) {
                        std::cout << '#';
                    } else if (y == height - 2 && std::abs(x - playerX) <= basketRadius) {
                        std::cout << '='; // Player basket
                    } else {
                        char symbol = ' ';
                        for (const auto& item : items) {
                            if (item.x == x && item.y == y) {
                                symbol = item.isBomb ? '*' : '$';
                                break;
                            }
                        }
                        std::cout << symbol;
                    }
                }
                std::cout << '\n';
            }
            std::cout << "Legend: [$] Coin (+1)   [*] Bomb   [=====] Basket\n";
            std::cout.flush();

            std::this_thread::sleep_for(std::chrono::milliseconds(110));
        }

        if (isGameOver && !quitApp) {
            std::cout << "\n====================================\n";
            std::cout << "   GAME OVER! (3 Bombs Collected)   \n";
            std::cout << "   Final Score: " << score << "\n";
            std::cout << "   Press [R] to Restart or [Q] to Quit\n";
            std::cout << "====================================\n";

            while (true) {
                int k = ReadKeyNonBlocking();
                if (k == 'r' || k == 'R') {
                    break;
                } else if (k == 'q' || k == 'Q') {
                    quitApp = true;
                    break;
                }
                std::this_thread::sleep_for(std::chrono::milliseconds(50));
            }
        }
    }

    return 0;
}